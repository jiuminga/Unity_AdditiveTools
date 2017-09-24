using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class SD_Type_Reflected : SD_Type
{
    private readonly Type reflectedType;
    public Type GetReflectedType() { return reflectedType; }

    private bool allPublicMembersReflected;
    private bool allNonPublicMembersReflected;

    public SD_Type_Reflected(Type type)
    {
        reflectedType = type;
        modifiers = type.IsNested ?
            (type.IsNestedPublic ? Modifiers.Public :
                type.IsNestedFamORAssem ? Modifiers.Internal | Modifiers.Protected :
                type.IsNestedAssembly ? Modifiers.Internal :
                type.IsNestedFamily ? Modifiers.Protected :
                Modifiers.Private)
            :
            (type.IsPublic ? Modifiers.Public :
                !type.IsVisible ? Modifiers.Internal :
                Modifiers.Private);
        if (type.IsAbstract && type.IsSealed)
            modifiers |= Modifiers.Static;
        else if (type.IsAbstract)
            modifiers |= Modifiers.Abstract;
        else if (type.IsSealed)
            modifiers |= Modifiers.Sealed;
        accessLevel = AccessLevelFromModifiers(modifiers);

        var assemblyDefinition = SD_Assembly.FromAssembly(type.Assembly);

        var generic = type.Name.IndexOf('`');
        name = generic < 0 ? type.Name : type.Name.Substring(0, generic);
        name = name.Replace("[*]", "[]");
        parentSymbol = string.IsNullOrEmpty(type.Namespace) ? assemblyDefinition.GlobalNamespace : assemblyDefinition.FindNamespace(type.Namespace);
        if (type.IsInterface)
            kind = SymbolKind.Interface;
        else if (type.IsEnum)
            kind = SymbolKind.Enum;
        else if (type.IsValueType)
            kind = SymbolKind.Struct;
        else if (type.IsClass)
        {
            kind = SymbolKind.Class;
            if (type.BaseType == typeof(System.MulticastDelegate))
            {
                kind = SymbolKind.Delegate;
            }
        }
        else
            kind = SymbolKind.None;

        if (type.IsGenericTypeDefinition)
        {
            var gtd = type.GetGenericTypeDefinition() ?? type;
            var tp = gtd.GetGenericArguments();
            var numGenericArgs = tp.Length;
            var declaringType = gtd.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
            {
                var parentArgs = declaringType.GetGenericArguments();
                numGenericArgs -= parentArgs.Length;
            }

            if (numGenericArgs > 0)
            {
                typeParameters = new List<SD_Type_Parameter>(numGenericArgs);
                for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
                {
                    var tpDef = new SD_Type_Parameter { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
                    typeParameters.Add(tpDef);
                }
            }
        }

        if (type.BaseType != null)
            baseType = ReflectedTypeReference.ForType(type.BaseType ?? typeof(object));

        interfaces = new List<SymbolReference>();
        var implements = type.GetInterfaces();
        for (var i = 0; i < implements.Length; ++i)
            interfaces.Add(ReflectedTypeReference.ForType(implements[i]));

        if (IsStatic && NumTypeParameters == 0 && !type.IsNested)
        {
            var attributes = System.Attribute.GetCustomAttributes(type);
            foreach (var attribute in attributes)
            {
                if (attribute is System.Runtime.CompilerServices.ExtensionAttribute)
                {
                    ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
                    break;
                }
            }
        }
    }

    private Dictionary<int, SymbolDefinition> importedMembers;
    public SymbolDefinition ImportReflectedMember(MemberInfo info)
    {
        if (info.MemberType == MemberTypes.Method && ((MethodInfo)info).IsPrivate)
            return null;
        if (info.MemberType == MemberTypes.Constructor && ((ConstructorInfo)info).IsPrivate)
            return null;
        if (info.MemberType == MemberTypes.Field && (((FieldInfo)info).IsPrivate || kind == SymbolKind.Enum && info.Name == "value__"))
            return null;
        if (info.MemberType == MemberTypes.NestedType && ((Type)info).IsNestedPrivate)
            return null;
        if (info.MemberType == MemberTypes.Property)
        {
            var p = (PropertyInfo)info;
            var get = p.GetGetMethod(true);
            var set = p.GetSetMethod(true);
            if ((get == null || get.IsPrivate) && (set == null || set.IsPrivate))
                return null;
        }
        if (info.MemberType == MemberTypes.Event)
        {
            var e = (EventInfo)info;
            var add = e.GetAddMethod(true);
            var remove = e.GetRemoveMethod(true);
            if ((add == null || add.IsPrivate) && (remove == null || remove.IsPrivate))
                return null;
        }

        SymbolDefinition imported = null;

        if (importedMembers == null)
            importedMembers = new Dictionary<int, SymbolDefinition>();
        else if (importedMembers.TryGetValue(info.MetadataToken, out imported))
            return imported;

        if (info.MemberType == MemberTypes.NestedType || info.MemberType == MemberTypes.TypeInfo)
        {
            imported = ImportReflectedType(info as Type);
        }
        else if (info.MemberType == MemberTypes.Method)
        {
            imported = ImportReflectedMethod(info as MethodInfo);
        }
        else if (info.MemberType == MemberTypes.Constructor)
        {
            imported = ImportReflectedConstructor(info as ConstructorInfo);
        }
        else
        {
            imported = new SD_Instance_Reflected(info, this);
        }

        members[imported.name, imported.kind != SymbolKind.MethodGroup ? imported.NumTypeParameters : 0] = imported;
        importedMembers[info.MetadataToken] = imported;
        return imported;
    }

    public override string GetName()
    {
        if (builtInTypes.ContainsValue(this))
            return (from x in builtInTypes where x.Value == this select x.Key).First();
        return base.GetName();
    }

    public override SymbolDefinition TypeOf()
    {
        if (kind != SymbolKind.Delegate)
            return this;

        GetParameters();
        return returnType.Definition;
    }

    public override List<SymbolDefinition> GetAllIndexers()
    {
        if (!allPublicMembersReflected || !allNonPublicMembersReflected)
            ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);

        return base.GetAllIndexers();
    }

    private static bool FilterByName(MemberInfo m, object filterCriteria)
    {
        var memberName = (string)filterCriteria;
        return m.Name == memberName || m.Name.Length > memberName.Length && m.Name.StartsWith(memberName, StringComparison.Ordinal) && m.Name[memberName.Length] == '`';
    }

    public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        SymbolDefinition member = null;
        if (!allPublicMembersReflected || !allNonPublicMembersReflected)
            ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
        if (!members.TryGetValue(memberName, numTypeParameters, out member))
            return null;

        if (asTypeOnly && member != null && !(member is TypeDefinitionBase))
            return null;
        return member;
    }

    public void ReflectAllMembers(BindingFlags flags)
    {
        flags |= BindingFlags.DeclaredOnly;

        var instaceMembers = reflectedType.GetMembers(flags | BindingFlags.Instance);
        foreach (var m in instaceMembers)
            if (m.MemberType != MemberTypes.Method || !((MethodInfo)m).IsSpecialName)
                ImportReflectedMember(m);

        var staticMembers = reflectedType.GetMembers(flags | BindingFlags.Static);
        foreach (var m in staticMembers)
            if (m.MemberType != MemberTypes.Method || !((MethodInfo)m).IsSpecialName)
                ImportReflectedMember(m);

        if ((flags & BindingFlags.Public) == BindingFlags.Public)
            allPublicMembersReflected = true;
        if ((flags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
            allNonPublicMembersReflected = true;
    }

    private ReflectedTypeReference returnType;
    private List<SD_Instance_Parameter> parameters;
    public override List<SD_Instance_Parameter> GetParameters()
    {
        if (kind != SymbolKind.Delegate)
            return null;

        if (parameters == null)
        {
            var invoke = reflectedType.GetMethod("Invoke");

            returnType = ReflectedTypeReference.ForType(invoke.ReturnType);

            parameters = new List<SD_Instance_Parameter>();
            foreach (var p in invoke.GetParameters())
            {
                var isByRef = p.ParameterType.IsByRef;
                var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                parameters.Add(new SD_Instance_Parameter
                {
                    kind = SymbolKind.Parameter,
                    parentSymbol = this,
                    name = p.Name,
                    type = ReflectedTypeReference.ForType(parameterType),
                    modifiers = isByRef ? (p.IsOut ? Modifiers.Out : Modifiers.Ref) : Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? Modifiers.Params : Modifiers.None,
                });
            }
        }

        return parameters;
    }

    private string delegateInfoText;
    public override string GetDelegateInfoText()
    {
        if (delegateInfoText == null)
        {
            var parameters = GetParameters();
            var returnType = TypeOf();

            delegateInfoText = returnType.GetName() + " " + GetName() + (parameters.Count == 1 ? "( " : "(");
            delegateInfoText += PrintParameters(parameters) + (parameters.Count == 1 ? " )" : ")");
        }

        return delegateInfoText;
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (!allPublicMembersReflected)
        {
            if (!allNonPublicMembersReflected)
                ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
            else
                ReflectAllMembers(BindingFlags.Public);
        }
        else if (!allNonPublicMembersReflected)
        {
            ReflectAllMembers(BindingFlags.NonPublic);
        }

        base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
    }

    //private Dictionary<BindingFlags, Dictionary<string, SymbolDefinition>> cachedMemberCompletions;
    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        if (!allPublicMembersReflected)
        {
            if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
                ReflectAllMembers(BindingFlags.Public | BindingFlags.NonPublic);
            else
                ReflectAllMembers(BindingFlags.Public);
        }
        else if (!allNonPublicMembersReflected && ((mask & AccessLevelMask.NonPublic) != 0 || (flags & BindingFlags.NonPublic) != 0))
        {
            ReflectAllMembers(BindingFlags.NonPublic);
        }

        base.GetMembersCompletionData(data, flags, mask, assembly);
    }
}
