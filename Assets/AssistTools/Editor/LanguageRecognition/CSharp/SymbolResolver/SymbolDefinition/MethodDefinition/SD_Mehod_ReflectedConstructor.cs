using System;
using System.Collections.Generic;
using System.Reflection;

public class SD_Mehod_ReflectedConstructor : MethodDefinition
{
    public SD_Mehod_ReflectedConstructor(ConstructorInfo constructorInfo, SymbolDefinition memberOf)
    {
        modifiers =
            constructorInfo.IsPublic ? Modifiers.Public :
            constructorInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
            constructorInfo.IsAssembly ? Modifiers.Internal :
            constructorInfo.IsFamily ? Modifiers.Protected :
            Modifiers.Private;
        if (constructorInfo.IsAbstract)
            modifiers |= Modifiers.Abstract;
        if (constructorInfo.IsStatic)
            modifiers |= Modifiers.Static;
        accessLevel = AccessLevelFromModifiers(modifiers);

        name = ".ctor";
        kind = SymbolKind.Constructor;
        parentSymbol = memberOf;

        returnType = new SymbolReference(memberOf);

        if (parameters == null)
            parameters = new List<SD_Instance_Parameter>();
        foreach (var p in constructorInfo.GetParameters())
        {
            var isByRef = p.ParameterType.IsByRef;
            var parameterType = isByRef ? p.ParameterType.GetElementType() : p.ParameterType;
            var parameterToAdd = new SD_Instance_Parameter
            {
                kind = SymbolKind.Parameter,
                parentSymbol = this,
                name = p.Name,
                type = ReflectedTypeReference.ForType(parameterType),
                modifiers = isByRef ? (p.IsOut ? Modifiers.Out : Modifiers.Ref) : Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? Modifiers.Params : Modifiers.None,
            };
            if (p.RawDefaultValue != DBNull.Value)
            {
                parameterToAdd.defaultValue = p.RawDefaultValue == null ? "null" : p.RawDefaultValue.ToString();
            }
            parameters.Add(parameterToAdd);
        }
    }
}

