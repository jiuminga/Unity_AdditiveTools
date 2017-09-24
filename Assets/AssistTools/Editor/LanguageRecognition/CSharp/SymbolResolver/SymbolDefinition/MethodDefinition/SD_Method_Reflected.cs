using System;
using System.Collections.Generic;
using System.Reflection;

public class SD_Method_Reflected : MethodDefinition
{
    public readonly MethodInfo reflectedMethodInfo;

    public SD_Method_Reflected(MethodInfo methodInfo, SymbolDefinition memberOf)
    {
        modifiers =
            methodInfo.IsPublic ? Modifiers.Public :
            methodInfo.IsFamilyOrAssembly ? Modifiers.Internal | Modifiers.Protected :
            methodInfo.IsAssembly ? Modifiers.Internal :
            methodInfo.IsFamily ? Modifiers.Protected :
            Modifiers.Private;
        if (methodInfo.IsAbstract)
            modifiers |= Modifiers.Abstract;
        if (methodInfo.IsVirtual)
            modifiers |= Modifiers.Virtual;
        if (methodInfo.IsStatic)
            modifiers |= Modifiers.Static;
        if (methodInfo.GetBaseDefinition().DeclaringType != methodInfo.DeclaringType)
            modifiers = (modifiers & ~Modifiers.Virtual) | Modifiers.Override;
        if (methodInfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false) && IsStatic)
        {
            var parentType = memberOf.parentSymbol as TypeDefinitionBase;
            if (parentType.kind == SymbolKind.Class && parentType.IsStatic && parentType.NumTypeParameters == 0)
            {
                isExtensionMethod = true;
                ++parentType.numExtensionMethods;
            }
        }
        accessLevel = AccessLevelFromModifiers(modifiers);

        reflectedMethodInfo = methodInfo;
        var genericMarker = methodInfo.Name.IndexOf('`');
        name = genericMarker < 0 ? methodInfo.Name : methodInfo.Name.Substring(0, genericMarker);
        parentSymbol = memberOf;

        var tp = methodInfo.GetGenericArguments();
        if (tp.Length > 0)
        {
            var numGenericArgs = tp.Length;
            typeParameters = new List<SD_Type_Parameter>(tp.Length);
            for (var i = tp.Length - numGenericArgs; i < tp.Length; ++i)
            {
                var tpDef = new SD_Type_Parameter { kind = SymbolKind.TypeParameter, name = tp[i].Name, parentSymbol = this };
                typeParameters.Add(tpDef);
            }
        }

        returnType = ReflectedTypeReference.ForType(methodInfo.ReturnType);

        if (parameters == null)
            parameters = new List<SD_Instance_Parameter>();
        var methodParameters = methodInfo.GetParameters();
        for (var i = 0; i < methodParameters.Length; ++i)
        {
            var p = methodParameters[i];

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
            if (i == 0 && isExtensionMethod)
                parameterToAdd.modifiers |= Modifiers.This;
            if (p.RawDefaultValue != DBNull.Value)
            {
                //var dv = Attribute.GetCustomAttribute(p, typeof(System.ComponentModel.DefaultValueAttribute));
                parameterToAdd.defaultValue = p.RawDefaultValue == null ? "null" : p.RawDefaultValue.ToString();
            }
            parameters.Add(parameterToAdd);
        }
    }
}

