using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
public class ReflectedTypeReference : SymbolReference
{
    protected Type reflectedType;
    protected ReflectedTypeReference(Type type)
    {
        reflectedType = type;
    }

    private static readonly Dictionary<Type, ReflectedTypeReference> allReflectedReferences = new Dictionary<Type, ReflectedTypeReference>();

    public static ReflectedTypeReference ForType(Type type)
    {
        ReflectedTypeReference result;
        if (allReflectedReferences.TryGetValue(type, out result))
            return result;
        result = new ReflectedTypeReference(type);
        allReflectedReferences[type] = result;
        return result;
    }

    public override SymbolDefinition Definition
    {
        get
        {
            if (_definition != null && !_definition.IsValid())
                _definition = null;

            if (_definition == null)
            {
                if (reflectedType.IsArray)
                {
                    var elementType = reflectedType.GetElementType();
                    var elementTypeDefinition = ReflectedTypeReference.ForType(elementType).Definition as TypeDefinitionBase;
                    var rank = reflectedType.GetArrayRank();
                    _definition = elementTypeDefinition.MakeArrayType(rank);
                    return _definition;
                }

                if (reflectedType.IsGenericParameter)
                {
                    var index = reflectedType.GenericParameterPosition;
                    var reflectedDeclaringMethod = reflectedType.DeclaringMethod as MethodInfo;
                    if (reflectedDeclaringMethod != null && reflectedDeclaringMethod.IsGenericMethod)
                    {
                        var declaringTypeRef = ForType(reflectedDeclaringMethod.DeclaringType);
                        var declaringType = declaringTypeRef.Definition as SD_Type_Reflected;
                        if (declaringType == null)
                            return _definition = SymbolDefinition.unknownType;
                        var methodName = reflectedDeclaringMethod.Name;
                        var typeArgs = reflectedDeclaringMethod.GetGenericArguments();
                        var numTypeArgs = typeArgs.Length;
                        var member = declaringType.FindName(methodName, numTypeArgs, false);
                        if (member == null && numTypeArgs > 0)
                            member = declaringType.FindName(methodName, 0, false);
                        if (member != null && member.kind == SymbolKind.MethodGroup)
                        {
                            var methodGroup = (SD_MethodGroup)member;
                            foreach (var m in methodGroup.methods)
                            {
                                var reflectedMethod = m as SD_Method_Reflected;
                                if (reflectedMethod != null && reflectedMethod.reflectedMethodInfo == reflectedDeclaringMethod)
                                {
                                    member = reflectedMethod;
                                    break;
                                }
                            }
                        }
                        var methodDefinition = member as MethodDefinition;
                        _definition = methodDefinition.typeParameters.ElementAtOrDefault(index);
                    }
                    else
                    {
                        var reflectedDeclaringType = reflectedType.DeclaringType;
                        while (true)
                        {
                            var parentType = reflectedDeclaringType.DeclaringType;
                            if (parentType == null)
                                break;
                            var count = parentType.GetGenericArguments().Length;
                            if (count <= index)
                            {
                                index -= count;
                                break;
                            }
                            reflectedDeclaringType = parentType;
                        }

                        var declaringTypeRef = ForType(reflectedDeclaringType);
                        var declaringType = declaringTypeRef.Definition as SD_Type;
                        if (declaringType == null)
                            return _definition = SymbolDefinition.unknownType;

                        _definition = declaringType.typeParameters[index];
                    }
                    return _definition;
                }

                if (reflectedType.IsGenericType && !reflectedType.IsGenericTypeDefinition)
                {
                    var reflectedTypeDef = reflectedType.GetGenericTypeDefinition();
                    var genericTypeDefRef = ForType(reflectedTypeDef);
                    var genericTypeDef = genericTypeDefRef.Definition as SD_Type;
                    if (genericTypeDef == null)
                        return _definition = SymbolDefinition.unknownType;

                    var reflectedTypeArgs = reflectedType.GetGenericArguments();
                    var numGenericArgs = reflectedTypeArgs.Length;
                    var declaringType = reflectedType.DeclaringType;
                    if (declaringType != null && declaringType.IsGenericType)
                    {
                        var parentArgs = declaringType.GetGenericArguments();
                        numGenericArgs -= parentArgs.Length;
                    }

                    var typeArguments = new ReflectedTypeReference[numGenericArgs];
                    for (int i = typeArguments.Length - numGenericArgs, j = 0; i < typeArguments.Length; ++i)
                        typeArguments[j++] = ForType(reflectedTypeArgs[i]);
                    _definition = genericTypeDef.ConstructType(typeArguments);
                    return _definition;
                }

                var tn = reflectedType.Name;
                SymbolDefinition declaringSymbol = null;

                if (reflectedType.IsNested)
                {
                    declaringSymbol = ForType(reflectedType.DeclaringType).Definition;
                }
                else
                {
                    var assemblyDefinition = SD_Assembly.FromAssembly(reflectedType.Assembly);
                    if (assemblyDefinition != null)
                        declaringSymbol = assemblyDefinition.FindNamespace(reflectedType.Namespace);
                }

                if (declaringSymbol != null && declaringSymbol.kind != SymbolKind.Error)
                {
                    var rankSpecifier = tn.IndexOf('[');
                    if (rankSpecifier > 0)
                        tn = tn.Substring(0, rankSpecifier);
                    var numTypeArgs = 0;
                    var genericMarkerIndex = tn.IndexOf('`');
                    if (genericMarkerIndex > 0)
                    {
                        numTypeArgs = int.Parse(tn.Substring(genericMarkerIndex + 1));
                        tn = tn.Substring(0, genericMarkerIndex);
                    }
                    _definition = declaringSymbol.FindName(tn, numTypeArgs, true);
                    if (_definition == null)
                    {
                        return null;
                    }
                    else if (rankSpecifier > 0)
                    {
                        var elementType = _definition as SD_Type;
                        if (elementType != null)
                        {
                            _definition = elementType.MakeArrayType(tn.Length - rankSpecifier - 1);
                        }
                        else
                        {
                            _definition = null;
                        }
                    }
                }
                if (_definition == null)
                    _definition = SymbolDefinition.unknownType;
            }
            return _definition;
        }
    }

    public override string ToString()
    {
        return reflectedType.FullName;
    }
}

