using System;
using System.Reflection;

public class SD_Instance_Reflected : SD_Instance
{
    private readonly MemberInfo memberInfo;

    public SD_Instance_Reflected(MemberInfo info, SymbolDefinition memberOf)
    {
        MethodInfo getMethodInfo = null;
        MethodInfo setMethodInfo = null;
        MethodInfo addMethodInfo = null;
        MethodInfo removeMethodInfo = null;

        switch (info.MemberType)
        {
            case MemberTypes.Constructor:
            case MemberTypes.Method:
                throw new InvalidOperationException();

            case MemberTypes.Field:
                var fieldInfo = (FieldInfo)info;
                modifiers =
                    fieldInfo.IsPublic ? Modifiers.Public :
                    fieldInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
                    fieldInfo.IsAssembly ? Modifiers.Internal :
                    fieldInfo.IsFamily ? Modifiers.Protected :
                    Modifiers.Private;
                if (fieldInfo.IsStatic)// && !fieldInfo.IsLiteral)
                    modifiers |= Modifiers.Static;
                break;

            case MemberTypes.Property:
                var propertyInfo = (PropertyInfo)info;
                getMethodInfo = propertyInfo.GetGetMethod(true);
                setMethodInfo = propertyInfo.GetSetMethod(true);
                modifiers = GetAccessorModifiers(getMethodInfo, setMethodInfo);
                break;

            case MemberTypes.Event:
                var eventInfo = (EventInfo)info;
                addMethodInfo = eventInfo.GetAddMethod(true);
                removeMethodInfo = eventInfo.GetRemoveMethod(true);
                modifiers = GetAccessorModifiers(addMethodInfo, removeMethodInfo);
                break;

            default:
                break;
        }
        accessLevel = AccessLevelFromModifiers(modifiers);

        memberInfo = info;
        var generic = info.Name.IndexOf('`');
        name = generic < 0 ? info.Name : info.Name.Substring(0, generic);
        parentSymbol = memberOf;
        switch (info.MemberType)
        {
            case MemberTypes.Field:
                kind = ((FieldInfo)info).IsLiteral ?
                    (memberOf.kind == SymbolKind.Enum ? SymbolKind.EnumMember : SymbolKind.ConstantField) :
                    SymbolKind.Field;
                break;
            case MemberTypes.Property:
                var indexParams = ((PropertyInfo)info).GetIndexParameters();
                kind = indexParams.Length > 0 ? SymbolKind.Indexer : SymbolKind.Property;
                if (getMethodInfo != null)
                {
                    var accessor = Create(SymbolKind.Accessor, "get");
                    accessor.modifiers = setMethodInfo != null ? GetAccessorModifiers(getMethodInfo) : modifiers;
                    AddMember(accessor);
                }
                if (setMethodInfo != null)
                {
                    var accessor = Create(SymbolKind.Accessor, "set");
                    accessor.modifiers = getMethodInfo != null ? GetAccessorModifiers(setMethodInfo) : modifiers;
                    AddMember(accessor);
                }
                break;
            case MemberTypes.Event:
                kind = SymbolKind.Event;
                if (addMethodInfo != null)
                {
                    var accessor = Create(SymbolKind.Accessor, "add");
                    accessor.modifiers = removeMethodInfo != null ? GetAccessorModifiers(addMethodInfo) : modifiers;
                    AddMember(accessor);
                }
                if (removeMethodInfo != null)
                {
                    var accessor = Create(SymbolKind.Accessor, "remove");
                    accessor.modifiers = addMethodInfo != null ? GetAccessorModifiers(removeMethodInfo) : modifiers;
                    AddMember(accessor);
                }
                break;
            default:
                throw new InvalidOperationException("Importing a non-supported member type!");
        }
    }

    private Modifiers GetAccessorModifiers(MethodInfo accessor1, MethodInfo accessor2)
    {
        var union = GetAccessorModifiers(accessor1) | GetAccessorModifiers(accessor2);
        var result = (union & Modifiers.Public) != 0 ? Modifiers.Public : union & (Modifiers.Internal | Modifiers.Protected);
        if (result == Modifiers.None)
            result = Modifiers.Private;
        result |= union & (Modifiers.Abstract | Modifiers.Virtual | Modifiers.Static);
        return result;
    }

    private Modifiers GetAccessorModifiers(MethodInfo accessor)
    {
        if (accessor == null)
            return Modifiers.Private;

        var modifiers =
            accessor.IsPublic ? Modifiers.Public :
            accessor.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
            accessor.IsAssembly ? Modifiers.Internal :
            accessor.IsFamily ? Modifiers.Protected :
            Modifiers.Private;
        if (accessor.IsAbstract)
            modifiers |= Modifiers.Abstract;
        if (accessor.IsVirtual)
            modifiers |= Modifiers.Virtual;
        if (accessor.IsStatic)
            modifiers |= Modifiers.Static;
        var baseDefinition = accessor.GetBaseDefinition();
        if (baseDefinition != null && baseDefinition.DeclaringType != accessor.DeclaringType)
            modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
        return modifiers;
    }

    public override SymbolDefinition TypeOf()
    {
        if (memberInfo.MemberType == MemberTypes.Constructor)
            return parentSymbol.TypeOf();

        if (type != null && (type.Definition == null || !type.Definition.IsValid()))
            type = null;

        if (type == null)
        {
            Type memberType = null;
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    memberType = ((FieldInfo)memberInfo).FieldType;
                    break;
                case MemberTypes.Property:
                    memberType = ((PropertyInfo)memberInfo).PropertyType;
                    break;
                case MemberTypes.Event:
                    memberType = ((EventInfo)memberInfo).EventHandlerType;
                    break;
                case MemberTypes.Method:
                    memberType = ((MethodInfo)memberInfo).ReturnType;
                    break;
            }
            type = ReflectedTypeReference.ForType(memberType);
        }

        return type != null ? type.Definition : unknownType;
    }
}

