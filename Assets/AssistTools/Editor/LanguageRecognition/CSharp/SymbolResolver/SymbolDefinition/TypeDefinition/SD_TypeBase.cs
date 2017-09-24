using System;
using System.Collections.Generic;
using System.Reflection;

public abstract class TypeDefinitionBase : SymbolDefinition
{
    protected SymbolDefinition thisReferenceCache;

    public int numExtensionMethods;

    protected bool convertingToBase; // Prevents infinite recursion

    public override Type GetRuntimeType()
    {
        if (Assembly == null || Assembly.assembly == null)
            return null;

        if (parentSymbol is TypeDefinitionBase)
        {
            Type parentType = parentSymbol.GetRuntimeType();
            if (parentType == null)
                return null;

            var result = parentType.GetNestedType(ReflectionName, BindingFlags.NonPublic | BindingFlags.Public);
            return result;
        }

        return Assembly.assembly.GetType(FullReflectionName);
    }

    public override SymbolDefinition TypeOf()
    {
        return this;
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        return this;
    }

    public virtual void InvalidateBaseType() { }

    public virtual List<SymbolReference> Interfaces()
    {
        return _emptyInterfaceList;
    }

    public virtual TypeDefinitionBase BaseType()
    {
        return this == builtInTypes_object ? null : builtInTypes_object;
    }

    protected virtual string RankString()
    {
        return string.Empty;
    }

    protected MethodDefinition defaultConstructor;
    public virtual MethodDefinition GetDefaultConstructor()
    {
        if (defaultConstructor == null)
        {
            defaultConstructor = new MethodDefinition
            {
                kind = SymbolKind.Constructor,
                parentSymbol = this,
                name = ".ctor",
                accessLevel = accessLevel,
                modifiers = modifiers & (Modifiers.Public | Modifiers.Internal | Modifiers.Protected),
            };
        }
        return defaultConstructor;
    }

    private Dictionary<int, SD_Type_Array> createdArrayTypes;
    public SD_Type_Array MakeArrayType(int arrayRank)
    {
        SD_Type_Array arrayType;
        if (createdArrayTypes == null)
            createdArrayTypes = new Dictionary<int, SD_Type_Array>();
        if (!createdArrayTypes.TryGetValue(arrayRank, out arrayType))
            createdArrayTypes[arrayRank] = arrayType = new SD_Type_Array(this, arrayRank);
        return arrayType;
    }

    private SD_Type createdNullableType;
    public SD_Type MakeNullableType()
    {
        if (createdNullableType == null)
        {
            createdNullableType = builtInTypes_Nullable.ConstructType(new[] { new SymbolReference(this) });
        }
        return createdNullableType;
    }

    public SymbolDefinition GetThisInstance()
    {
        if (thisReferenceCache == null)
        {
            if (IsStatic)
                return thisReferenceCache = unknownType;
            thisReferenceCache = new SD_Instance_This(new SymbolReference(this));
        }
        return thisReferenceCache;
    }

    private bool resolvingInBase = false;
    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);

        if (!resolvingInBase && leaf.ResolvedSymbol == null)
        {
            resolvingInBase = true;

            var baseType = BaseType();
            var interfaces = Interfaces();

            if (!asTypeOnly && interfaces != null && (kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
            {
                foreach (var i in interfaces)
                {
                    i.Definition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
                    if (leaf.ResolvedSymbol != null)
                    {
                        resolvingInBase = false;
                        return;
                    }
                }
            }

            if (baseType != null && baseType != this)
            {
                baseType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
            }

            resolvingInBase = false;
        }
    }

    public virtual bool DerivesFrom(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return false;

        return DerivesFromRef(ref otherType);
    }

    public virtual bool DerivesFromRef(ref TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return false;

        var otherTypeAsConstructed = otherType as SD_Type_Constructed;
        if (otherTypeAsConstructed != null)
            otherType = otherTypeAsConstructed.genericTypeDefinition;

        if (this == otherType)
            return true;

        if (BaseType() != null)
            return BaseType().DerivesFromRef(ref otherType);

        return false;
    }

    protected override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
    {
        var indexers = GetAllIndexers();

        // TODO: Resolve overloads

        return indexers != null ? indexers[0] : null;
    }

    public virtual List<SymbolDefinition> GetAllIndexers()
    {
        List<SymbolDefinition> indexers = null;
        foreach (var m in members)
            if (m.kind == SymbolKind.Indexer)
            {
                if (indexers == null)
                    indexers = new List<SymbolDefinition>();
                indexers.Add(m);
            }
        return indexers;
    }

    public void ListOverrideCandidates(List<MethodDefinition> methods, SD_Assembly context)
    {
        if (completionsFromBase)
            return;
        completionsFromBase = true;

        var baseType = BaseType();
        if (baseType != null && (baseType.kind == SymbolKind.Class || baseType.kind == SymbolKind.Struct))
            baseType.ListOverrideCandidates(methods, context);

        completionsFromBase = false;

        var accessLevelMask = AccessLevelMask.Public | AccessLevelMask.Protected;
        if (Assembly.InternalsVisibleIn(context))
            accessLevelMask |= AccessLevelMask.Internal;

        for (var i = members.Count; i-- > 0; )
        {
            var member = members[i];
            if (member.kind == SymbolKind.MethodGroup)
            {
                var asMethodGroup = member as SD_MethodGroup;
                if (asMethodGroup != null)
                {
                    foreach (var method in asMethodGroup.methods)
                    {
                        if ((method.IsVirtual || method.IsAbstract) && method.IsAccessible(accessLevelMask))
                        {
                            methods.Add(method);
                        }
                    }
                }
            }
        }
    }

    private bool completionsFromBase = false;
    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        base.GetMembersCompletionData(data, flags, mask, assembly);

        if (completionsFromBase)
            return;
        completionsFromBase = true;

        var baseType = BaseType();
        var interfaces = Interfaces();
        if (flags != BindingFlags.Static && (kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
            foreach (var i in interfaces)
                i.Definition.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
        if (baseType != null && (kind != SymbolKind.Enum || flags != BindingFlags.Static) &&
            (baseType.kind != SymbolKind.Interface || kind == SymbolKind.Interface || kind == SymbolKind.TypeParameter))
        {
            baseType.GetMembersCompletionData(data, flags, mask & ~AccessLevelMask.Private, assembly);
        }

        completionsFromBase = false;
    }

    internal virtual TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        return null;
    }

    public virtual bool CanConvertTo(TypeDefinitionBase otherType)
    {
        return ConvertTo(otherType) != null;
    }

    public virtual TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType == this)
            return this;

        if (otherType is SD_Type_Parameter)
            return this;

        if (otherType == builtInTypes_object)
            return otherType;

        if (this == builtInTypes_int && (
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_uint && (
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_byte && (
            otherType == builtInTypes_short ||
            otherType == builtInTypes_ushort ||
            otherType == builtInTypes_int ||
            otherType == builtInTypes_uint ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_sbyte && (
            otherType == builtInTypes_short ||
            otherType == builtInTypes_int ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_short && (
            otherType == builtInTypes_int ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_ushort && (
            otherType == builtInTypes_int ||
            otherType == builtInTypes_uint ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if ((this == builtInTypes_long || this == builtInTypes_ulong) &&
            (otherType == builtInTypes_float || otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_float &&
            otherType == builtInTypes_double)
            return otherType;
        if (this == builtInTypes_char &&
            otherType == builtInTypes_string)
            return otherType;

        if (DerivesFromRef(ref otherType))
            return otherType;

        return null;
    }
}
