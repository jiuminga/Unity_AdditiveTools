using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class SD_Type_Constructed : SD_Type
{
    public readonly SD_Type genericTypeDefinition;
    public readonly SymbolReference[] typeArguments;

    public SD_Type_Constructed(SD_Type definition, SymbolReference[] arguments)
    {
        name = definition.name;
        kind = definition.kind;
        parentSymbol = definition.parentSymbol;
        genericTypeDefinition = definition;

        if (definition.typeParameters != null && arguments != null)
        {
            typeParameters = definition.typeParameters;
            typeArguments = new SymbolReference[typeParameters.Count];
            for (var i = 0; i < typeArguments.Length && i < arguments.Length; ++i)
                typeArguments[i] = arguments[i];
        }
    }

    public override SD_Type_Constructed ConstructType(SymbolReference[] typeArgs)
    {
        var result = genericTypeDefinition.ConstructType(typeArgs);
        result.parentSymbol = parentSymbol;
        return result;
    }

    public override SymbolDefinition TypeOf()
    {
        if (kind != SymbolKind.Delegate)
            return base.TypeOf();

        var result = genericTypeDefinition.TypeOf() as TypeDefinitionBase;
        result = result.SubstituteTypeParameters(this);
        return result;
    }

    public override SymbolDefinition GetGenericSymbol()
    {
        return genericTypeDefinition;
    }

    public override TypeDefinitionBase TypeOfTypeParameter(SD_Type_Parameter tp)
    {
        if (typeParameters != null)
        {
            var index = typeParameters.IndexOf(tp);
            if (index >= 0)
            {
                if (typeArguments[index] == null)
                    return unknownType;
                else
                    return typeArguments[index].Definition as TypeDefinitionBase ?? tp;
            }
        }
        return base.TypeOfTypeParameter(tp);
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        var target = this;
        var parentType = parentSymbol as TypeDefinitionBase;
        if (parentType != null)
        {
            parentType = parentType.SubstituteTypeParameters(context);
            var constructedParent = parentType as SD_Type_Constructed;
            if (constructedParent != null)
                target = constructedParent.GetConstructedMember(this.genericTypeDefinition) as SD_Type_Constructed;
        }

        if (typeArguments == null)
            return target;

        var constructNew = false;
        var newArguments = new SymbolReference[typeArguments.Length];
        for (var i = 0; i < newArguments.Length; ++i)
        {
            newArguments[i] = typeArguments[i];
            var original = typeArguments[i] != null ? typeArguments[i].Definition as TypeDefinitionBase : null;
            if (original == null)
                continue;
            var substitute = original.SubstituteTypeParameters(target);
            substitute = substitute.SubstituteTypeParameters(context);
            if (substitute != original)
            {
                newArguments[i] = new SymbolReference(substitute);
                constructNew = true;
            }
        }
        if (!constructNew)
            return target;
        return ConstructType(newArguments);
    }

    internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        if (argumentType.kind == SymbolKind.LambdaExpression)
            return argumentType.BindTypeArgument(typeArgument, TypeOf() as TypeDefinitionBase);

        var convertedArgument = argumentType.ConvertTo(this);

        //TypeDefinitionBase convertedArgument = this;
        //if (!argumentType.DerivesFromRef(ref convertedArgument))
        //	return base.BindTypeArgument(typeArgument, argumentType);

        var argumentAsConstructedType = convertedArgument as SD_Type_Constructed;
        if (argumentAsConstructedType != null && GetGenericSymbol() == argumentAsConstructedType.GetGenericSymbol())
        {
            TypeDefinitionBase inferedType = null;
            for (int i = 0; i < NumTypeParameters; ++i)
            {
                var fromConstructedType = argumentAsConstructedType.typeArguments[i].Definition as TypeDefinitionBase;
                if (fromConstructedType != null)
                {
                    var bindTarget = typeArguments[i].Definition as TypeDefinitionBase;
                    var boundTypeArgument = bindTarget.BindTypeArgument(typeArgument, fromConstructedType);
                    if (boundTypeArgument != null)
                    {
                        if (inferedType == null || inferedType.CanConvertTo(boundTypeArgument))
                            inferedType = boundTypeArgument;
                        else if (!boundTypeArgument.CanConvertTo(inferedType))
                            return null;
                    }
                }
            }

            if (inferedType != null)
                return inferedType;
        }
        return base.BindTypeArgument(typeArgument, argumentType);
    }

    public override List<SymbolReference> Interfaces()
    {
        if (interfaces == null)
            BaseType();
        return interfaces;
    }

    public override TypeDefinitionBase BaseType()
    {
        if (baseType != null && (baseType.Definition == null || !baseType.Definition.IsValid()) ||
            interfaces != null && interfaces.Exists(x => x.Definition == null || !x.Definition.IsValid()))
        {
            baseType = null;
            interfaces = null;
        }

        if (interfaces == null)
        {
            var baseTypeDef = genericTypeDefinition.BaseType();
            baseType = baseTypeDef != null ? new SymbolReference(baseTypeDef.SubstituteTypeParameters(this)) : null;

            interfaces = new List<SymbolReference>(genericTypeDefinition.Interfaces());
            for (var i = 0; i < interfaces.Count; ++i)
            {
                var interfaceDefinition = interfaces[i].Definition as TypeDefinitionBase;
                if (interfaceDefinition != null)
                    interfaces[i] = new SymbolReference(interfaceDefinition.SubstituteTypeParameters(this));
            }
        }
        return baseType != null ? baseType.Definition as TypeDefinitionBase : base.BaseType();
    }

    public override List<SD_Instance_Parameter> GetParameters()
    {
        return genericTypeDefinition.GetParameters();
    }

    public override bool CanConvertTo(TypeDefinitionBase otherType)
    {
        return ConvertTo(otherType) != null;
    }

    public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType is SD_Type_Parameter)
            return this;

        if (genericTypeDefinition == otherType)
            return this;

        var otherGenericType = otherType.GetGenericSymbol() as TypeDefinitionBase;
        if (genericTypeDefinition == otherGenericType)
        {
            var otherConstructedType = otherType as SD_Type_Constructed;
            var otherTypeTypeArgs = otherConstructedType.typeArguments;

            var convertedTypeArgs = new List<SymbolReference>(typeArguments.Length);
            for (var i = 0; i < typeArguments.Length; i++)
            {
                var typeArgument = typeArguments[i].Definition as TypeDefinitionBase;
                if (typeArgument == null)
                    typeArgument = otherTypeTypeArgs[i].Definition as TypeDefinitionBase;
                else
                    typeArgument = typeArgument.ConvertTo(otherTypeTypeArgs[i].Definition as TypeDefinitionBase);
                if (typeArgument == null)
                    break;

                var typeReference = new SymbolReference(typeArgument);
                convertedTypeArgs.Add(typeReference);
            }

            if (convertedTypeArgs.Count == typeArguments.Length)
            {
                var convertedType = genericTypeDefinition.ConstructType(convertedTypeArgs.ToArray());
                return convertedType;
            }
        }

        if (convertingToBase)
            return null;
        convertingToBase = true;

        var baseTypeDefinition = BaseType();

        if (otherType.kind == SymbolKind.Interface)
        {
            for (int i = 0; i < interfaces.Count; i++)
            {
                var interfaceTpe = interfaces[i];
                var convertedToInterface = ((TypeDefinitionBase)interfaceTpe.Definition).ConvertTo(otherType);
                if (convertedToInterface != null)
                {
                    convertingToBase = false;
                    return convertedToInterface;
                }
            }
        }

        if (baseTypeDefinition != null)
        {
            var converted = baseTypeDefinition.ConvertTo(otherType);
            if (converted != null)
            {
                convertingToBase = false;
                return converted;
            }
        }

        convertingToBase = false;

        return null;
    }

    public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return false;

        if (genericTypeDefinition == otherType)
        {
            otherType = this;
            return true;
        }

        var baseType = BaseType();

        if (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter)
        {
            foreach (var i in interfaces)
                if (((TypeDefinitionBase)i.Definition).DerivesFromRef(ref otherType))
                {
                    otherType = otherType.SubstituteTypeParameters(this);
                    return true;
                }
        }

        if (baseType != null && baseType.DerivesFromRef(ref otherType))
        {
            otherType = otherType.SubstituteTypeParameters(this);
            return true;
        }

        return false;
    }

    public override bool DerivesFrom(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return false;

        return genericTypeDefinition.DerivesFrom(otherType);
    }

    public override string GetName()
    {
        if (typeArguments == null || typeArguments.Length == 0)
            return name;

        var sb = new StringBuilder();
        sb.Append(name);
        var comma = "<";
        for (var i = 0; i < typeArguments.Length; ++i)
        {
            sb.Append(comma);
            if (typeArguments[i] != null)
                sb.Append(typeArguments[i].Definition.GetName());
            comma = ", ";
        }
        sb.Append('>');
        return sb.ToString();
    }

    public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        return genericTypeDefinition.FindName(memberName, numTypeParameters, asTypeOnly);
    }

    public Dictionary<SymbolDefinition, SymbolDefinition> constructedMembers;

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        genericTypeDefinition.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);

        var genericMember = leaf.ResolvedSymbol;
        if (genericMember == null)// || genericMember is MethodGroupDefinition)// !genericMember.IsGeneric)
            return;

        SymbolDefinition constructed;
        if (constructedMembers != null && constructedMembers.TryGetValue(genericMember, out constructed))
            leaf.ResolvedSymbol = constructed;
        else
            leaf.ResolvedSymbol = GetConstructedMember(genericMember);

        if (asTypeOnly && !(leaf.ResolvedSymbol is TypeDefinitionBase))
            leaf.ResolvedSymbol = null;
    }

    public SymbolDefinition GetConstructedMember(SymbolDefinition member)
    {
        var parent = member.parentSymbol;
        if (parent is SD_MethodGroup)
            parent = parent.parentSymbol;

        if (genericTypeDefinition != parent)
        {
            return member;
        }

        SymbolDefinition constructed;
        if (constructedMembers == null)
            constructedMembers = new Dictionary<SymbolDefinition, SymbolDefinition>();
        else if (constructedMembers.TryGetValue(member, out constructed))
            return constructed;

        constructed = ConstructMember(member);
        constructedMembers[member] = constructed;
        return constructed;
    }

    private SymbolDefinition ConstructMember(SymbolDefinition member)
    {
        SymbolDefinition symbol;
        if (member is SD_Instance)
        {
            symbol = new SD_Instance_Constructed(member as SD_Instance);
        }
        if (member is SD_Type)
        {
            symbol = (member as SD_Type).ConstructType(null);// new ConstructedTypeDefinition(member as TypeDefinition, null);
        }
        else
        {
            symbol = new SD_ConstructedReference(member);
        }
        symbol.parentSymbol = this;
        return symbol;
    }

    public override bool IsSameType(TypeDefinitionBase type)
    {
        if (type == this)
            return true;

        var constructedType = type as SD_Type_Constructed;
        if (constructedType == null)
            return false;

        if (genericTypeDefinition != constructedType.genericTypeDefinition)
            return false;

        for (var i = 0; i < typeArguments.Length; ++i)
            if (!typeArguments[i].Definition.IsSameType(constructedType.typeArguments[i].Definition as TypeDefinitionBase))
                return false;

        return true;
    }

    protected override SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
    {
        var indexers = GetAllIndexers();

        // TODO: Resolve overloads
        return indexers != null ? indexers[indexers.Count - 1] : null;
    }

    public override List<SymbolDefinition> GetAllIndexers()
    {
        List<SymbolDefinition> indexers = genericTypeDefinition.GetAllIndexers();
        if (indexers != null)
        {
            for (var i = 0; i < indexers.Count; ++i)
            {
                var member = indexers[i];
                member = GetConstructedMember(member);
                indexers[i] = member;
            }
        }
        return indexers;
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        var dataFromDefinition = new Dictionary<string, SymbolDefinition>();
        genericTypeDefinition.GetMembersCompletionData(dataFromDefinition, flags, mask, assembly);
        foreach (var entry in dataFromDefinition)
        {
            if (!data.ContainsKey(entry.Key))
            {
                var member = GetConstructedMember(entry.Value);
                data.Add(entry.Key, member);
            }
        }
    }
}
