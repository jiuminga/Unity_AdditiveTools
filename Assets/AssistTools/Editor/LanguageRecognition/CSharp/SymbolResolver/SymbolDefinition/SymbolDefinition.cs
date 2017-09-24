using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Debug = UnityEngine.Debug;

public class SymbolDefinition
{
    public static readonly SymbolDefinition resolvedChildren = new SymbolDefinition { kind = SymbolKind.None };
    public static readonly SymbolDefinition nullLiteral = new SD_Instance_NullLit { kind = SymbolKind.Null };
    public static readonly SymbolDefinition contextualKeyword = new SymbolDefinition { kind = SymbolKind.Null };
    public static readonly SD_Type unknownType = new SD_Type { name = "unknown type", kind = SymbolKind.Error };
    public static readonly SD_Type circularBaseType = new SD_Type { name = "circular base type", kind = SymbolKind.Error };
    public static readonly SymbolDefinition unknownSymbol = new SymbolDefinition { name = "unknown symbol", kind = SymbolKind.Error };
    public static readonly SymbolDefinition thisInStaticMember = new SymbolDefinition { name = "cannot use 'this' in static member", kind = SymbolKind.Error };
    public static readonly SymbolDefinition baseInStaticMember = new SymbolDefinition { name = "cannot use 'base' in static member", kind = SymbolKind.Error };

    protected static readonly List<SD_Instance_Parameter> _emptyParameterList = new List<SD_Instance_Parameter>();
    protected static readonly List<SymbolReference> _emptyInterfaceList = new List<SymbolReference>();

    public static Dictionary<string, TypeDefinitionBase> builtInTypes;

    public static SD_Type builtInTypes_int;
    public static SD_Type builtInTypes_uint;
    public static SD_Type builtInTypes_byte;
    public static SD_Type builtInTypes_sbyte;
    public static SD_Type builtInTypes_short;
    public static SD_Type builtInTypes_ushort;
    public static SD_Type builtInTypes_long;
    public static SD_Type builtInTypes_ulong;
    public static SD_Type builtInTypes_float;
    public static SD_Type builtInTypes_double;
    public static SD_Type builtInTypes_decimal;
    public static SD_Type builtInTypes_char;
    public static SD_Type builtInTypes_string;
    public static SD_Type builtInTypes_bool;
    public static SD_Type builtInTypes_object;
    public static SD_Type builtInTypes_void;

    public static SD_Type builtInTypes_Array;
    public static SD_Type builtInTypes_Nullable;
    public static SD_Type builtInTypes_IEnumerable;
    public static SD_Type builtInTypes_IEnumerable_1;
    public static SD_Type builtInTypes_Exception;

    public SymbolKind kind;
    public string name;
    public UnityEngine.Texture2D cachedIcon;

    public SymbolDefinition parentSymbol;

    public Modifiers modifiers;
    public AccessLevel accessLevel;

    /// <summary>
    /// Zero, one, or more declarations defining this symbol
    /// </summary>
    /// <remarks>Check for null!!!</remarks>
    public List<SymbolDeclaration> declarations;
    public SymbolList members = new SymbolList();

    private static readonly string[] lambdaBodyRulePath = new[] {
        "argumentValue",
        "expression",
        "nonAssignmentExpression",
        "lambdaExpression",
        "lambdaExpressionBody"
    };

    #region Flag
    public bool IsInstanceMember
    {
        get
        {
            return !IsStatic && kind != SymbolKind.ConstantField && !(this is TypeDefinitionBase);
        }
    }

    public bool IsSealed
    {
        get
        {
            return (modifiers & Modifiers.Sealed) != 0;
        }
    }

    public virtual bool IsStatic
    {
        get
        {
            return (modifiers & Modifiers.Static) != 0;
        }
        set
        {
            if (value)
                modifiers |= Modifiers.Static;
            else
                modifiers &= ~Modifiers.Static;
        }
    }

    public bool IsPublic
    {
        get
        {
            return (modifiers & Modifiers.Public) != 0 ||
                (kind == SymbolKind.Namespace) ||
                parentSymbol != null && (
                    parentSymbol.parentSymbol != null
                    && (kind == SymbolKind.Method || kind == SymbolKind.Indexer)
                    && (parentSymbol.parentSymbol.kind == SymbolKind.Interface)
                    ||
                    (kind == SymbolKind.Property || kind == SymbolKind.Event)
                    && (parentSymbol.kind == SymbolKind.Interface)
                );
        }
        set
        {
            if (value)
                modifiers |= Modifiers.Public;
            else
                modifiers &= ~Modifiers.Public;
        }
    }

    public bool IsInternal
    {
        get
        {
            return (modifiers & Modifiers.Internal) != 0 ||
                kind != SymbolKind.Namespace && (modifiers & Modifiers.Public) == 0 && parentSymbol != null && parentSymbol.kind == SymbolKind.Namespace;
        }
        set
        {
            if (value)
                modifiers |= Modifiers.Internal;
            else
                modifiers &= ~Modifiers.Internal;
        }
    }

    public bool IsProtected
    {
        get
        {
            return (modifiers & Modifiers.Protected) != 0;
        }
        set
        {
            if (value)
                modifiers |= Modifiers.Protected;
            else
                modifiers &= ~Modifiers.Protected;
        }
    }

    public bool IsPrivate
    {
        get
        {
            return (modifiers & (Modifiers.Protected | Modifiers.Internal | Modifiers.Public)) == 0;
        }
    }

    public bool IsAbstract
    {
        get
        {
            return (modifiers & Modifiers.Abstract) != 0;
        }
        set
        {
            if (value)
                modifiers |= Modifiers.Abstract;
            else
                modifiers &= ~Modifiers.Abstract;
        }
    }

    public bool IsPartial
    {
        get { return (modifiers & Modifiers.Partial) != 0; }
    }

    public virtual bool IsExtensionMethod
    {
        get { return false; }
    }
    #endregion

    public string FullName
    {
        get
        {
            if (parentSymbol != null)
            {
                var parentFullName = (parentSymbol is SD_MethodGroup)
                    ? (parentSymbol.parentSymbol ?? unknownSymbol).FullName
                    : parentSymbol.FullName;
                if (string.IsNullOrEmpty(name))
                    return parentFullName;
                if (string.IsNullOrEmpty(parentFullName))
                    return name;
                return parentFullName + '.' + name;
            }
            return name;
        }
    }

    public string FullReflectionName
    {
        get
        {
            if (parentSymbol != null)
            {
                var parentFullName = (parentSymbol is SD_MethodGroup)
                    ? (parentSymbol.parentSymbol ?? unknownSymbol).FullReflectionName
                    : parentSymbol.FullReflectionName;
                if (string.IsNullOrEmpty(ReflectionName))
                    return parentFullName;
                if (string.IsNullOrEmpty(parentFullName))
                    return ReflectionName;
                return parentFullName + '.' + ReflectionName;
            }
            return ReflectionName;
        }
    }

    public string ReflectionName
    {
        get
        {
            var tp = GetTypeParameters();
            return tp != null && tp.Count > 0 ? name + "`" + tp.Count : name;
        }
    }

    public string UnityHelpName
    {
        get
        {
            if (kind == SymbolKind.TypeParameter)
                return null;

            var result = FullName;
            if (result == null)
                return null;
            if (result.StartsWith("UnityEngine.", StringComparison.Ordinal))
                result = result.Substring("UnityEngine.".Length);
            else if (result.StartsWith("UnityEditor.", StringComparison.Ordinal))
                result = result.Substring("UnityEditor.".Length);
            else
                return null;

            if (kind == SymbolKind.Indexer)
                result = result.Substring(0, result.LastIndexOf('.') + 1) + "Index_operator";
            else if (kind == SymbolKind.Constructor)
                result = result.Substring(0, result.LastIndexOf('.')) + "-ctor";
            else if ((kind == SymbolKind.Field || kind == SymbolKind.Property) && parentSymbol.kind != SymbolKind.Enum)
                result = result.Substring(0, result.LastIndexOf('.')) + "-" + name;

            if (kind == SymbolKind.Class && NumTypeParameters > 0)
                name += "_" + NumTypeParameters;

            return result;
        }
    }

    public string XmlDocsName
    {
        get
        {
            var sb = new StringBuilder();
            switch (kind)
            {
                case SymbolKind.Namespace:
                    sb.Append("N:");
                    sb.Append(FullName);
                    break;
                case SymbolKind.Class:
                case SymbolKind.Struct:
                case SymbolKind.Interface:
                case SymbolKind.Enum:
                case SymbolKind.Delegate:
                    sb.Append("T:");
                    sb.Append(FullReflectionName);
                    break;
                case SymbolKind.Field:
                case SymbolKind.ConstantField:
                    sb.Append("F:");
                    sb.Append(FullReflectionName);
                    break;
                case SymbolKind.Property:
                    sb.Append("P:");
                    sb.Append(FullReflectionName);
                    break;
                case SymbolKind.Indexer:
                    sb.Append("P:");
                    sb.Append(parentSymbol.FullReflectionName);
                    sb.Append(".Item");
                    break;
                case SymbolKind.Method:
                case SymbolKind.Operator:
                    sb.Append("M:");
                    sb.Append(FullReflectionName);
                    break;
                case SymbolKind.Constructor:
                    sb.Append("M:");
                    sb.Append(parentSymbol.FullReflectionName);
                    sb.Append(".#ctor");
                    break;
                case SymbolKind.Destructor:
                    sb.Append("M:");
                    sb.Append(parentSymbol.FullReflectionName);
                    sb.Append(".Finalize");
                    break;
                case SymbolKind.Event:
                    sb.Append("E:");
                    sb.Append(FullReflectionName);
                    break;
                default:
                    return null;
            }
            var parameters = GetParameters();
            if (kind != SymbolKind.Delegate && parameters != null && parameters.Count > 0)
            {
                sb.Append("(");
                for (var i = 0; i < parameters.Count; ++i)
                {
                    var p = parameters[i];
                    if (i > 0)
                        sb.Append(",");
                    var t = p.TypeOf();
                    if (t.kind == SymbolKind.TypeParameter)
                    {
                        sb.Append('`');
                        var tp = t as SD_Type_Parameter;
                        var tpIndex = tp.parentSymbol.IndexOfTypeParameter(tp);
                        sb.Append(tpIndex);
                    }
                    else
                    {
                        sb.Append(t.FullReflectionName);
                    }
                    var a = t as SD_Type_Array;
                    if (a != null)
                    {
                        if (a.rank == 1)
                        {
                            sb.Append("[]");
                        }
                        else
                        {
                            sb.Append("[0:");
                            for (var j = 1; j < a.rank; ++j)
                                sb.Append(",0:");
                            sb.Append("]");
                        }
                    }
                    else if (p.IsRef || p.IsOut)
                        sb.Append("@");
                    if (p.IsOptional)
                        sb.Append("!");
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }

    public SD_Assembly Assembly
    {
        get
        {
            var assembly = this;
            while (assembly != null)
            {
                var result = assembly as SD_Assembly;
                if (result != null)
                    return result;
                assembly = assembly.parentSymbol;
            }
            return null;
        }
    }

    public int NumTypeParameters
    {
        get
        {
            var typeParameters = GetTypeParameters();
            return typeParameters != null ? typeParameters.Count : 0;
        }
    }

    protected string tooltipText;
    private bool tooltipAsExtensionMethod;

    public static AccessLevel AccessLevelFromModifiers(Modifiers modifiers)
    {
        if ((modifiers & Modifiers.Public) != 0)
            return AccessLevel.Public;
        if ((modifiers & Modifiers.Protected) != 0)
        {
            if ((modifiers & Modifiers.Internal) != 0)
                return AccessLevel.ProtectedOrInternal;
            return AccessLevel.Protected;
        }
        if ((modifiers & Modifiers.Internal) != 0)
            return AccessLevel.Internal;
        if ((modifiers & Modifiers.Private) != 0)
            return AccessLevel.Private;
        return AccessLevel.None;
    }

    public static string DecodeId(string name)
    {
        if (!string.IsNullOrEmpty(name) && name[0] == '@')
            return name.Substring(1);
        return name;
    }

    public bool IsValid()
    {
        if (declarations == null)
        {
            if (this is SD_Type_Reflected || this is SD_Method_Reflected || this is SD_Mehod_ReflectedConstructor || this is SD_Instance_Reflected)
            {
                return Assembly != null;
            }

            return true; // kind != SymbolKind.Error;
        }

        for (var i = declarations.Count; i-- > 0; )
        {
            var declaration = declarations[i];
            if (!declaration.IsValid())
            {
                declarations.RemoveAt(i);
                if (declaration.scope != null)
                {
                    declaration.scope.RemoveDeclaration(declaration);
                    ++LR_SyntaxTree.resolverVersion;
                    if (LR_SyntaxTree.resolverVersion == 0)
                        ++LR_SyntaxTree.resolverVersion;
                }
            }
        }

        return declarations.Count > 0;
    }

    public SymbolDefinition Rebind()
    {
        if (kind == SymbolKind.Namespace)
            return Assembly.FindNamespace(FullName);

        if (parentSymbol == null)
            return this;

        var newParent = parentSymbol.Rebind();
        if (newParent == null)
            return null;

        var tp = GetTypeParameters();
        var numTypeParams = tp != null ? tp.Count : 0;
        var symbolIsType = this is TypeDefinitionBase;
        SymbolDefinition newSymbol = newParent.FindName(name, numTypeParams, symbolIsType);
        if (newSymbol == null)
        {
            if (newParent.kind == SymbolKind.MethodGroup)
            {
                var mg = newParent as SD_MethodGroup;
                if (mg == null)
                {
                    var generic = newParent.GetGenericSymbol();
                    if (generic != null)
                        mg = generic as SD_MethodGroup;
                }
                if (mg != null)
                {
                    var signature = GetGenericSymbol().PrintParameters(GetParameters(), true);
                    foreach (var m in mg.methods)
                    {
                        var sig = m.PrintParameters(m.GetParameters(), true);
                        if (sig == signature)
                        {
                            newSymbol = m;
                            break;
                        }
                    }
                }
            }
#if SI3_WARNINGS
			if (newSymbol == null)
			{
				Debug.LogWarning(GetTooltipText() + " not found in " + newParent.GetTooltipText());
				return null;
			}
#endif
        }
        return newSymbol;
    }

    public virtual Type GetRuntimeType()
    {
        if (parentSymbol == null)
            return null;
        return parentSymbol.GetRuntimeType();
    }

    public static SymbolDefinition Create(SymbolDeclaration declaration)
    {
        var symbolName = declaration.Name;
        if (symbolName != null)
            symbolName = DecodeId(symbolName);

        var definition = Create(declaration.kind, symbolName);
        declaration.definition = definition;

        if (declaration.parseTreeNode != null)
        {
            definition.modifiers = declaration.modifiers;
            definition.accessLevel = AccessLevelFromModifiers(declaration.modifiers);

            if (definition.declarations == null)
                definition.declarations = new List<SymbolDeclaration>();
            definition.declarations.Add(declaration);
        }

        var nameNode = declaration.NameNode();
        if (nameNode is SyntaxTreeNode_Leaf)
            nameNode.SetDeclaredSymbol(definition);

        return definition;
    }

    public static SymbolDefinition Create(SymbolKind kind, string name)
    {
        SymbolDefinition definition;

        switch (kind)
        {
            case SymbolKind.LambdaExpression:
                definition = new SD_type_LambaExpr
                {
                    name = name,
                };
                break;

            case SymbolKind.Parameter:
                definition = new SD_Instance_Parameter
                {
                    name = name,
                };
                break;

            case SymbolKind.ForEachVariable:
            case SymbolKind.FromClauseVariable:
            case SymbolKind.Variable:
            case SymbolKind.Field:
            case SymbolKind.ConstantField:
            case SymbolKind.LocalConstant:
            case SymbolKind.Property:
            case SymbolKind.Event:
            case SymbolKind.CatchParameter:
            case SymbolKind.EnumMember:
                definition = new SD_Instance
                {
                    name = name,
                };
                break;

            case SymbolKind.Indexer:
                definition = new SD_Instance_Indexer
                {
                    name = name,
                };
                break;

            case SymbolKind.Struct:
            case SymbolKind.Class:
            case SymbolKind.Enum:
            case SymbolKind.Interface:
                definition = new SD_Type
                {
                    name = name,
                };
                break;

            case SymbolKind.Delegate:
                definition = new SD_Type_Delegate
                {
                    name = name,
                };
                break;

            case SymbolKind.Namespace:
                definition = new SD_NameSpace
                {
                    name = name,
                };
                break;

            case SymbolKind.Method:
                definition = new MethodDefinition
                {
                    name = name,
                };
                break;

            case SymbolKind.Constructor:
                definition = new MethodDefinition
                {
                    name = ".ctor",
                };
                break;

            case SymbolKind.MethodGroup:
                definition = new SD_MethodGroup
                {
                    name = name,
                };
                break;

            case SymbolKind.TypeParameter:
                definition = new SD_Type_Parameter
                {
                    name = name,
                };
                break;

            case SymbolKind.Accessor:
                definition = new SymbolDefinition
                {
                    name = name,
                };
                break;

            default:
                definition = new SymbolDefinition
                {
                    name = name,
                };
                break;
        }

        definition.kind = kind;

        return definition;
    }

    public virtual string GetName()
    {
        var typeParameters = GetTypeParameters();
        if (typeParameters == null || typeParameters.Count == 0)
            return name;

        var sb = new StringBuilder();
        sb.Append(name);
        sb.Append('<');
        sb.Append(typeParameters[0].GetName());
        for (var i = 1; i < typeParameters.Count; ++i)
        {
            sb.Append(", ");
            sb.Append(typeParameters[i].GetName());
        }
        sb.Append('>');
        return sb.ToString();
    }

    public virtual SymbolDefinition TypeOf()
    {
        return this;
    }

    public virtual SymbolDefinition GetGenericSymbol()
    {
        return this;
    }

    public virtual TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        Debug.Log("Not a type! Can't substitute type of: " + GetTooltipText());
        return null;
    }

    public static Dictionary<Type, SD_Type_Reflected> reflectedTypes = new Dictionary<Type, SD_Type_Reflected>();

    public TypeDefinitionBase ImportReflectedType(Type type)
    {
        SD_Type_Reflected reflectedType;
        if (reflectedTypes.TryGetValue(type, out reflectedType))
            return reflectedType;

        if (type.IsArray)
        {
            var elementType = ImportReflectedType(type.GetElementType());
            var arrayType = elementType.MakeArrayType(type.GetArrayRank());
            return arrayType;
        }

        if ((type.IsGenericType || type.ContainsGenericParameters) && !type.IsGenericTypeDefinition)
        {
            var arguments = type.GetGenericArguments();
            var numGenericArgs = arguments.Length;
            var declaringType = type.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
            {
                var parentArgs = declaringType.GetGenericArguments();
                numGenericArgs -= parentArgs.Length;
            }

            var argumentRefs = new List<ReflectedTypeReference>(numGenericArgs);
            for (var i = arguments.Length - numGenericArgs; i < arguments.Length; ++i)
                argumentRefs.Add(ReflectedTypeReference.ForType(arguments[i]));

            var typeDefinitionRef = ReflectedTypeReference.ForType(type.GetGenericTypeDefinition());
            var typeDefinition = typeDefinitionRef.Definition as SD_Type;
            var constructedType = typeDefinition.ConstructType(argumentRefs.ToArray());
            return constructedType;
        }

        if (type.IsGenericParameter)
        {
            UnityEngine.Debug.LogError("Importing reflected generic type parameter " + type.FullName);
        }

        reflectedTypes[type] = reflectedType = new SD_Type_Reflected(type);
        members[reflectedType.name, reflectedType.NumTypeParameters] = reflectedType;
        reflectedType.parentSymbol = this;
        return reflectedType;
    }

    public SymbolDefinition ImportReflectedMethod(MethodInfo info)
    {
        var importedReflectionName = info.Name;
        SymbolDefinition methodGroup;
        if (!members.TryGetValue(importedReflectionName, 0, out methodGroup))
        {
            methodGroup = Create(SymbolKind.MethodGroup, importedReflectionName);
            methodGroup.parentSymbol = this;
            members[importedReflectionName, 0] = methodGroup;
        }
        var imported = new SD_Method_Reflected(info, methodGroup);
        ((SD_MethodGroup)methodGroup).AddMethod(imported);
        return methodGroup;
    }

    public SymbolDefinition ImportReflectedConstructor(ConstructorInfo info)
    {
        var imported = new SD_Mehod_ReflectedConstructor(info, this);
        members[".ctor", 0] = imported;
        return imported;
    }

    public void AddMember(SymbolDefinition symbol)
    {
        symbol.parentSymbol = this;
        if (!string.IsNullOrEmpty(symbol.name))
        {
            var declaration = symbol.declarations != null && symbol.declarations.Count == 1 ? symbol.declarations[0] : null;
            if (declaration != null && declaration.numTypeParameters > 0)
                members[declaration.Name, declaration.numTypeParameters] = symbol;
            else
                members[symbol.name, symbol.NumTypeParameters] = symbol;
        }
    }

    public SymbolDefinition AddMember(SymbolDeclaration symbol)
    {
        var member = Create(symbol);
        var symbolName = member.name;
        if (member.kind == SymbolKind.Method)
        {
            SymbolDefinition methodGroup = null;
            if (!members.TryGetValue(symbolName, 0, out methodGroup) || !(methodGroup is SD_MethodGroup))
            {
                methodGroup = AddMember(new SymbolDeclaration(symbolName)
                {
                    kind = SymbolKind.MethodGroup,
                    modifiers = symbol.modifiers,
                    parseTreeNode = symbol.parseTreeNode,
                    scope = symbol.scope,
                });
            }
            var asMethodGroup = methodGroup as SD_MethodGroup;
            if (asMethodGroup != null)
            {
                asMethodGroup.AddMethod((MethodDefinition)member);
            }
        }
        else
        {
            if (member.kind == SymbolKind.Delegate)
            {
                var memberAsDelegate = (SD_Type_Delegate)member;
                memberAsDelegate.returnType = new SymbolReference(symbol.parseTreeNode.ChildAt(1));
            }
            AddMember(member);
        }

        //TODO:unknow
        //if (member.IsPartial)
        //{
        //    if (member is TypeDefinitionBase)
        //        FGTextBufferManager.FindOtherTypeDeclarationParts(symbol);
        //}

        return member;
    }

    public virtual SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        var parentNamespace = this as SD_NameSpace;

        SymbolDefinition definition;
        if (parentNamespace != null && symbol is Declaration_Namespace)
        {
            var qnNode = symbol.parseTreeNode.NodeAt(1);
            if (qnNode == null)
                return null;

            for (var i = 0; i < qnNode.NumValidNodes - 2; i += 2)
            {
                var ns = qnNode.ChildAt(i).Print();
                var childNS = parentNamespace.FindName(ns, 0, false);
                if (childNS == null)
                {
                    childNS = new SD_NameSpace
                    {
                        kind = SymbolKind.Namespace,
                        name = ns,
                        accessLevel = AccessLevel.Public,
                        modifiers = Modifiers.Public,
                    };
                    parentNamespace.AddMember(childNS);
                }
                parentNamespace = childNS as SD_NameSpace;
                if (parentNamespace == null)
                    break;
            }
        }

        var addToSymbol = parentNamespace ?? this;
        if (!addToSymbol.members.TryGetValue(symbol.Name, symbol.kind == SymbolKind.Method ? 0 : symbol.numTypeParameters, out definition) ||
            symbol.kind == SymbolKind.Method && definition is SD_MethodGroup ||
            definition is SD_Instance_Reflected || definition is SD_Type_Reflected ||
            definition is SD_Method_Reflected || definition is SD_Mehod_ReflectedConstructor ||
            !definition.IsValid())
        {
            if (definition != null &&
                (definition is SD_Instance_Reflected || definition is SD_Type_Reflected ||
                    definition is SD_Method_Reflected || definition is SD_Mehod_ReflectedConstructor)
                && definition != symbol.definition)
            {
                definition.Invalidate();
            }
            definition = addToSymbol.AddMember(symbol);
        }
        else
        {
            if (definition.kind == SymbolKind.Namespace && symbol.kind == SymbolKind.Namespace)
            {
                if (definition.declarations == null)
                    definition.declarations = new List<SymbolDeclaration>();
                definition.declarations.Add(symbol);
            }
            else if (symbol.IsPartial && definition.declarations != null && definition.declarations.Count > 0)
            {
                var definitionAsType = definition as TypeDefinitionBase;
                if (definitionAsType != null)
                {
                    definitionAsType.InvalidateBaseType();
                }
                definition.declarations.Add(symbol);
            }
            else
            {
                definition = addToSymbol.AddMember(symbol);
            }
        }

        symbol.definition = definition;

        var nameNode = symbol.NameNode();
        if (nameNode != null)
        {
            var leaf = nameNode as SyntaxTreeNode_Leaf;
            if (leaf == null)
            {
                var node = (SyntaxTreeNode_Rule)nameNode;
                if (node.RuleName == "memberName")
                {
                    node = node.NodeAt(0);
                    if (node != null)
                    {
                        node = node.NodeAt(-1);
                        if (node != null)
                        {
                            if (node.RuleName == "qidStart")
                            {
                                if (node.NumValidNodes < 3)
                                    leaf = node.LeafAt(0);
                                else
                                    leaf = node.LeafAt(2);
                            }
                            else
                            {
                                node = node.NodeAt(0);
                                if (node != null)
                                    leaf = node.LeafAt(1);
                            }
                        }
                    }
                }
            }
            if (leaf != null)
                leaf.SetDeclaredSymbol(definition);
        }

        return definition;
    }

    private void Invalidate()
    {
        parentSymbol = null;
        if (members != null)
        {
            foreach (var member in members)
                member.Invalidate();
        }
    }

    public virtual void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.Method)
        {
            // TODO: There's no need to RemoveAll - there can only be one
            members.RemoveAll((SymbolDefinition x) =>
            {
                if (x.declarations == null)
                    return false;
                if (x.kind == SymbolKind.MethodGroup)
                {
                    var mg = x as SD_MethodGroup;
                    mg.RemoveDeclaration(symbol);
                    if (mg.methods.Count == 0)
                    {
                        mg.declarations.Clear();
                        mg.parentSymbol = null;
                        return true;
                    }
                }
                return false;
            });
        }
        else
        {
            var index = members.FindIndex(x =>
            {
                if (x.declarations == null)
                    return false;
                if (x.kind == SymbolKind.MethodGroup)
                    return false;
                return x.declarations.Contains(symbol);
            });
            if (index >= 0)
            {
                var member = members[index];
                member.declarations.Remove(symbol);
                if (member.declarations.Count == 0)
                {
                    members.RemoveAt(index);
                }
                else
                {
                    var firstDeclarationKind = member.declarations[0].kind;
                    if (member.kind != firstDeclarationKind)
                    {
                        if ((firstDeclarationKind == SymbolKind.Class ||
                            firstDeclarationKind == SymbolKind.Struct ||
                            firstDeclarationKind == SymbolKind.Interface) &&
                            member.IsPartial && member is TypeDefinitionBase)
                        {
                            member.kind = firstDeclarationKind;
                        }
                    }
                }
            }
        }
    }

    public override string ToString()
    {
        return kind + " " + name;
    }

    public virtual string CompletionDisplayString(string styledName)
    {
        return styledName;
    }

    public virtual string GetDelegateInfoText() { return GetTooltipText(); }

    public string PrintParameters(List<SD_Instance_Parameter> parameters, bool singleLine = false)
    {
        if (parameters == null || tooltipAsExtensionMethod && parameters.Count == 1)
            return "";

        var parametersText = "";
        var comma = !singleLine && parameters.Count > (tooltipAsExtensionMethod ? 2 : 1) ? "\n    " : "";
        var nextComma = !singleLine && parameters.Count > (tooltipAsExtensionMethod ? 2 : 1) ? ",\n    " : ", ";
        for (var i = (tooltipAsExtensionMethod ? 1 : 0); i < parameters.Count; ++i)
        {
            var param = parameters[i];

            if (param == null)
                continue;
            var typeOfP = param.TypeOf() as TypeDefinitionBase;
            if (typeOfP == null)
                continue;
            typeOfP = typeOfP.SubstituteTypeParameters(this);

            if (typeOfP == null)
                continue;
            parametersText += comma;
            if (param.IsThisParameter)
                parametersText += "this ";
            else if (param.IsRef)
                parametersText += "ref ";
            else if (param.IsOut)
                parametersText += "out ";
            else if (param.IsParametersArray)
                parametersText += "params ";
            parametersText += typeOfP.GetName() + ' ' + param.name;
            if (param.defaultValue != null)
                parametersText += " = " + param.defaultValue;
            comma = nextComma;
        }
        if (!singleLine && parameters.Count > 1)
            parametersText += '\n';
        return parametersText;
    }

    public string GetTooltipTextAsExtensionMethod()
    {
        string result = "";
        try
        {
            tooltipAsExtensionMethod = true;
            result = GetTooltipText();
        }
        finally
        {
            tooltipAsExtensionMethod = false;
        }
        return result;
    }

    public virtual string GetTooltipText()
    {
        if (kind == SymbolKind.Null)
            return null;

        if (kind == SymbolKind.Error)
            return name;

        var kindText = string.Empty;
        switch (kind)
        {
            case SymbolKind.Namespace: return tooltipText = "namespace " + FullName;
            case SymbolKind.Constructor: kindText = "(constructor) "; break;
            case SymbolKind.Destructor: kindText = "(destructor) "; break;
            case SymbolKind.ConstantField:
            case SymbolKind.LocalConstant: kindText = "(constant) "; break;
            case SymbolKind.Property: kindText = "(property) "; break;
            case SymbolKind.Event: kindText = "(event) "; break;
            case SymbolKind.Variable:
            case SymbolKind.ForEachVariable:
            case SymbolKind.FromClauseVariable:
            case SymbolKind.CatchParameter: kindText = "(local variable) "; break;
            case SymbolKind.Parameter: kindText = "(parameter) "; break;
            case SymbolKind.Delegate: kindText = "delegate "; break;
            case SymbolKind.MethodGroup: kindText = "(method group) "; break;
            case SymbolKind.Accessor: kindText = "(accessor) "; break;
            case SymbolKind.Label: return tooltipText = "(label) " + name;
            case SymbolKind.Method: kindText = IsExtensionMethod ? "(extension) " : ""; break;
        }

        var typeOf = kind == SymbolKind.Accessor || kind == SymbolKind.MethodGroup ? null : TypeOf();
        var typeName = string.Empty;
        if (typeOf != null && kind != SymbolKind.Namespace && kind != SymbolKind.Constructor && kind != SymbolKind.Destructor)
        {
            var ctx = (typeOf.kind == SymbolKind.Delegate ? typeOf : parentSymbol) as SD_Type_Constructed;
            if (ctx != null)
                typeOf = ((TypeDefinitionBase)typeOf).SubstituteTypeParameters(ctx);
            typeName = typeOf.GetName() + " ";

            if (typeOf.kind != SymbolKind.TypeParameter)
                for (var parentType = typeOf.parentSymbol as TypeDefinitionBase; parentType != null; parentType = parentType.parentSymbol as TypeDefinitionBase)
                    typeName = parentType.GetName() + '.' + typeName;
        }

        var parameters = GetParameters();

        var parentText = string.Empty;
        var parent = parentSymbol is SD_MethodGroup ? parentSymbol.parentSymbol : parentSymbol;
        if ((parent is TypeDefinitionBase &&
                parent.kind != SymbolKind.Delegate && kind != SymbolKind.TypeParameter && parent.kind != SymbolKind.LambdaExpression)
            || parent is SD_NameSpace)
        {
            var parentName = parent.GetName();
            if (kind == SymbolKind.Constructor)
            {
                var typeParent = parent.parentSymbol as TypeDefinitionBase;
                parentName = typeParent != null ? typeParent.GetName() : null;
            }
            else if (kind == SymbolKind.Method && tooltipAsExtensionMethod)
            {
                var typeOfThisParameter = parameters[0].TypeOf();
                if (typeOfThisParameter != null)
                    typeOfThisParameter = typeOfThisParameter.SubstituteTypeParameters(this);
                parentName = typeOfThisParameter != null ? typeOfThisParameter.GetName() : null;
            }
            if (!string.IsNullOrEmpty(parentName))
                parentText = parentName + ".";
        }

        var nameText = GetName();

        var parametersText = string.Empty;
        string parametersEnd = null;

        if (kind == SymbolKind.Method)
        {
            nameText += (parameters.Count == (tooltipAsExtensionMethod ? 2 : 1) ? "( " : "(");
            parametersEnd = (parameters.Count == (tooltipAsExtensionMethod ? 2 : 1) ? " )" : ")");
        }
        else if (kind == SymbolKind.Constructor)
        {
            nameText = parent.name + '(';
            parametersEnd = ")";
        }
        else if (kind == SymbolKind.Destructor)
        {
            nameText = "~" + parent.name + "()";
        }
        else if (kind == SymbolKind.Indexer)
        {
            nameText = (parameters.Count == 1 ? "this[ " : "this[");
            parametersEnd = (parameters.Count == 1 ? " ]" : "]");
        }
        else if (kind == SymbolKind.Delegate)
        {
            nameText += (parameters.Count == 1 ? "( " : "(");
            parametersEnd = (parameters.Count == 1 ? " )" : ")");
        }

        if (parameters != null)
        {
            parametersText = PrintParameters(parameters);
        }

        tooltipText = kindText + typeName + parentText + nameText + parametersText + parametersEnd;

        tooltipText += DebugValue();

        if (typeOf != null && typeOf.kind == SymbolKind.Delegate)
        {
            tooltipText += "\n\nDelegate info\n";
            tooltipText += typeOf.GetDelegateInfoText();
        }

        var xmlDocs = GetXmlDocs();
        if (!string.IsNullOrEmpty(xmlDocs))
        {
            tooltipText += "\n\n" + xmlDocs;
        }

        return tooltipText;
    }

    protected string DebugValue()
    {
        var typeOf = TypeOf() as TypeDefinitionBase;
        if (kind == SymbolKind.Field || kind == SymbolKind.Property)
        {
            if (!(parentSymbol is TypeDefinitionBase))
                return "";

            var runtimeType = parentSymbol.GetRuntimeType();
            if (runtimeType == null)
                return "";

            if (runtimeType.ContainsGenericParameters)
                return "";

            object value;

            if (!IsStatic)
            {
                var isScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(runtimeType);
                var isComponent = typeof(UnityEngine.Component).IsAssignableFrom(runtimeType);
                if (isScriptableObject || isComponent)
                {
                    const BindingFlags instanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    UnityEngine.Object[] allInstances = null;
                    string result = "";
                    if (isComponent)
                    {
                        allInstances = UnityEditor.Selection.GetFiltered(runtimeType, UnityEditor.SelectionMode.ExcludePrefab);
                        if (allInstances.Length > 0)
                        {
                            result = "\n    in " + allInstances.Length + " selected scene objects";
                        }
                        else
                        {
                            allInstances = UnityEngine.Object.FindObjectsOfType(runtimeType);
                            if (allInstances.Length > 0)
                                result = "\n    in " + allInstances.Length + " active scene objects";
                        }
                    }
                    if (allInstances == null || allInstances.Length == 0)
                    {
                        allInstances = UnityEngine.Resources.FindObjectsOfTypeAll(runtimeType);
                        result = "\n    in " + allInstances.Length + " instances";
                    }

                    var fieldInfo = kind == SymbolKind.Field ? runtimeType.GetField(name, instanceMember) : null;
                    var propertyInfo = kind == SymbolKind.Property ? runtimeType.GetProperty(name, instanceMember) : null;
                    if (fieldInfo == null && propertyInfo == null)
                        return result;
                    try
                    {
                        for (var i = 0; i < Math.Min(allInstances.Length, 10); ++i)
                        {
                            value = fieldInfo != null
                                ? fieldInfo.GetValue(allInstances[i])
                                : propertyInfo.GetValue(allInstances[i], null);
                            result += DebugPrintValue(typeOf, value, "\n    " + (
                                allInstances[i].name == ""
                                ? allInstances[i].ToString()
                                : "\"" + allInstances[i].name + "\" (" + allInstances[i].GetHashCode() + ")") + ": ");
                        }
#if SI3_WARNINGS
					} catch (Exception e) {
						Debug.LogException(e);
					}
#else
                    }
                    catch { }
#endif
                    return result;
                }
                return "";
            }

            const BindingFlags staticMember = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (kind == SymbolKind.Field)
            {
                var fieldInfo = runtimeType.GetField(name, staticMember);
                if (fieldInfo == null)
                    return "";
                try
                {
                    value = fieldInfo.GetValue(null);
                }
                catch
                {
                    return "";
                }
            }
            else if (kind == SymbolKind.Property)
            {
                var propertyInfo = runtimeType.GetProperty(name, staticMember);
                if (propertyInfo == null)
                    return "";
                try
                {
                    value = propertyInfo.GetValue(null, null);
                }
                catch
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
            return DebugPrintValue(typeOf, value, "\n    = ");
        }
        return "";
    }

    protected string DebugPrintValue(TypeDefinitionBase typeOf, object value, string header)
    {
        if (value == null)
            return header + "null;";

        if (typeOf == builtInTypes_bool)
            return header + ((bool)value ? "true;" : "false;");
        if (typeOf == builtInTypes_int ||
            typeOf == builtInTypes_short ||
            typeOf == builtInTypes_sbyte)
            return header + value + ";";
        if (typeOf == builtInTypes_uint ||
            typeOf == builtInTypes_ushort ||
            typeOf == builtInTypes_byte)
            return header + value + "u;";
        if (typeOf == builtInTypes_long)
            return header + value + "L;";
        if (typeOf == builtInTypes_ulong)
            return header + value + "UL;";
        if (typeOf == builtInTypes_float)
            return header + value + "f;";
        if (typeOf == builtInTypes_char)
            return header + "'" + value + "';";
        if (typeOf == builtInTypes_string)
        {
            string s = "";
            try
            {
                s = value as string;
            }
            catch { }
            if (s.Length > 100)
                s = s.Substring(0, 100) + "...";
            var nl = s.IndexOfAny(new[] { '\r', '\n' });
            if (nl >= 0)
                s = s.Substring(0, nl) + "...";
            return header + "\"" + s + "\";";
        }

        var asEnumerable = value as System.Collections.IEnumerable;
        if (asEnumerable != null)
        {
            var asArray = value as Array;
            if (asArray != null)
                return header + "{ Length = " + asArray.Length + " }";
            var asCollection = value as System.Collections.ICollection;
            if (asCollection != null)
                return header + "{ Count = " + asCollection.Count + " }";
            var countProperty = value.GetType().GetProperty("Count");
            if (countProperty != null)
            {
                var count = countProperty.GetValue(value, null);
                return header + "{ Count = " + count + " }";
            }
        }

        var str = value.ToString();
        if (str.Length > 100)
            str = str.Substring(0, 100) + "...";
        var newLine = str.IndexOfAny(new[] { '\r', '\n' });
        if (newLine >= 0)
            str = str.Substring(0, newLine) + "...";
        return header + "{ " + str + " }";
    }

    public virtual List<SD_Instance_Parameter> GetParameters()
    {
        return null;
    }

    public virtual List<SD_Type_Parameter> GetTypeParameters()
    {
        return null;
    }

    protected string GetXmlDocs()
    {
#if UNITY_WEBPLAYER && !UNITY_5_0
		return null;
#else
        string result = null;

        var unityName = UnityHelpName;
        if (unityName != null)
        {
            if (UnitySymbols.summaries.TryGetValue(unityName, out result))
                return result;
            return null;
        }
        return result;
#endif
    }

    protected int IndexOfTypeParameter(SD_Type_Parameter tp)
    {
        var typeParams = GetTypeParameters();
        var index = typeParams != null ? typeParams.IndexOf(tp) : -1;
        if (index < 0)
            return parentSymbol != null ? parentSymbol.IndexOfTypeParameter(tp) : -1;
        for (var parent = parentSymbol; parent != null; parent = parent.parentSymbol)
        {
            typeParams = parent.GetTypeParameters();
            if (typeParams != null)
                index += typeParams.Count;
        }
        return index;
    }

    public string RelativeName(Scope_Base context)
    {
        if (context == null)
            return FullName;

        foreach (var kv in builtInTypes)
            if (kv.Value == this)
                return kv.Key;

        var thisPath = new List<SymbolDefinition>();
        for (var parent = this; parent != null; parent = parent.parentSymbol)
        {
            if (parent is SD_MethodGroup)
                parent = parent.parentSymbol;
            if (!string.IsNullOrEmpty(parent.name))
                thisPath.Add(parent);
        }

        var contextPath = new List<SymbolDefinition>();
        var contextScope = context;
        while (contextScope != null)
        {
            var asNamespaceScope = contextScope as Scope_Namespace;
            if (asNamespaceScope != null)
            {
                var nsDefinition = asNamespaceScope.definition;
                while (nsDefinition != null && !string.IsNullOrEmpty(nsDefinition.name))
                {
                    contextPath.Add(nsDefinition);
                    nsDefinition = nsDefinition.parentSymbol as SD_NameSpace;
                }
                break;
            }
            else
            {
                var asBodyScope = contextScope as Scope_Body;
                if (asBodyScope != null)
                {
                    var scopeDefinition = asBodyScope.definition;
                    switch (scopeDefinition.kind)
                    {
                        case SymbolKind.Class:
                        case SymbolKind.Struct:
                        case SymbolKind.Interface:
                            contextPath.Add(scopeDefinition);
                            break;
                    }
                }
            }

            contextScope = contextScope.parentScope;
        }

        while (contextPath.Count > 0 && thisPath.Count > 0 && contextPath[contextPath.Count - 1] == thisPath[thisPath.Count - 1])
        {
            contextPath.RemoveAt(contextPath.Count - 1);
            thisPath.RemoveAt(thisPath.Count - 1);
        }

        if (thisPath.Count <= 1)
            return name;

        SD_NameSpace thisNamespace = null;
        var index = thisPath.Count;
        while (index-- > 0)
        {
            var namespaceDefinition = thisPath[index] as SD_NameSpace;
            if (namespaceDefinition == null)
                break;
            thisNamespace = namespaceDefinition;
        }
        if (index >= 0 && thisNamespace != null && thisNamespace.parentSymbol != null)
        {
            ++index;
            var thisNamespaceName = thisNamespace.FullName;

            var contextNamespaceScope = context.EnclosingNamespaceScope();
            while (contextNamespaceScope != null)
            {
                var importedNamespaces = contextNamespaceScope.declaration.importedNamespaces;
                for (var i = importedNamespaces.Count; i-- > 0; )
                {
                    if (importedNamespaces[i].Definition.FullName == thisNamespaceName)
                    {
                        thisPath.RemoveRange(index, thisPath.Count - index);
                        goto namespaceIsImported;
                    }
                }
                contextNamespaceScope = contextNamespaceScope.parentScope as Scope_Namespace;
            }
        }

    namespaceIsImported:

        var sb = new StringBuilder();
        for (var i = thisPath.Count; i-- > 0; )
        {
            sb.Append(thisPath[i].name);
            var asConstructedType = thisPath[i] as SD_Type_Constructed;
            if (asConstructedType != null)
            {
                var typeArguments = asConstructedType.typeArguments;
                if (typeArguments != null && typeArguments.Length > 0)
                {
                    var comma = "<";
                    for (var j = 0; j < typeArguments.Length; ++j)
                    {
                        sb.Append(comma);
                        if (typeArguments[j] != null)
                            sb.Append(typeArguments[j].Definition.RelativeName(context));
                        comma = ", ";
                    }
                    sb.Append('>');
                }
            }
            if (i > 0)
                sb.Append('.');
        }
        return sb.ToString();
    }

    public string Dump()
    {
        var sb = new StringBuilder();
        Dump(sb, string.Empty);
        return sb.ToString();
    }

    protected virtual void Dump(StringBuilder sb, string indent)
    {
        sb.AppendLine(indent + kind + " " + name + " (" + GetType() + ")");

        foreach (var member in members)
            member.Dump(sb, indent + "  ");
    }

    public virtual void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;

        var id = DecodeId(leaf.token.text);

        SymbolDefinition definition;
        if (!members.TryGetValue(id, numTypeArgs, out definition))
        {
            return;
        }
        if (definition != null && definition.kind != SymbolKind.Namespace && !(definition is TypeDefinitionBase))
        {
            if (asTypeOnly)
                return;
            if (leaf.Parent != null && leaf.Parent.RuleName == "typeOrGeneric")
                leaf.m_sSemanticError = "Type expected";
        }

        leaf.ResolvedSymbol = definition;
    }

    public virtual void ResolveAttributeMember(SyntaxTreeNode_Leaf leaf, Scope_Base context)
    {
        leaf.ResolvedSymbol = null;

        var id = leaf.token.text;
        leaf.ResolvedSymbol = FindName(id, 0, true) ?? FindName(id + "Attribute", 0, true);
    }

    public virtual SymbolDefinition ResolveMethodOverloads(SyntaxTreeNode_Rule argumentListNode, SymbolReference[] typeArgs, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        throw new InvalidOperationException();
    }

    public static SymbolDefinition ResolveNodeAsConstructor(SyntaxTreeNode_Base oceNode, Scope_Base scope, SymbolDefinition asMemberOf)
    {
        if (asMemberOf == null)
            return null;

        var node = oceNode as SyntaxTreeNode_Rule;
        if (node == null || node.NumValidNodes == 0)
            return null;

        var node1 = node.RuleName == "arguments" ? node : node.NodeAt(0);
        if (node1 == null)
            return null;

        var constructor = asMemberOf.FindName(".ctor", 0, false);
        if (constructor == null || constructor.parentSymbol != asMemberOf)
            constructor = ((TypeDefinitionBase)asMemberOf).GetDefaultConstructor();
        if (constructor is SD_MethodGroup)
        {
            if (node1.RuleName == "arguments")
                constructor = ResolveNode(node1, scope, constructor);
        }
        else if (node1.RuleName == "arguments")
        {
            for (var i = 1; i < node1.NumValidNodes - 1; ++i)
                ResolveNode(node1.ChildAt(i), scope, constructor);
        }

        if (node.RuleName != "arguments" && node.NumValidNodes == 2)
            ResolveNode(node.ChildAt(1));

        return constructor;
    }

    public static SymbolDefinition EnumerableElementType(SyntaxTreeNode_Rule node)
    {
        var enumerableExpr = ResolveNode(node);
        if (enumerableExpr != null)
        {
            var arrayType = enumerableExpr.TypeOf() as SD_Type_Array;
            if (arrayType != null)
            {
                if (arrayType.rank > 0 && arrayType.elementType != null)
                    return arrayType.elementType;
            }
            else
            {
                var enumerableType = enumerableExpr.TypeOf() as TypeDefinitionBase;
                if (enumerableType != null)
                {
                    TypeDefinitionBase iEnumerableGenericTypeDef = builtInTypes_IEnumerable_1;
                    if (enumerableType.DerivesFromRef(ref iEnumerableGenericTypeDef))
                    {
                        var asGenericEnumerable = iEnumerableGenericTypeDef as SD_Type_Constructed;
                        if (asGenericEnumerable != null)
                            return asGenericEnumerable.typeArguments[0].Definition;
                    }

                    var iEnumerableTypeDef = builtInTypes_IEnumerable;
                    if (enumerableType.DerivesFrom(iEnumerableTypeDef))
                        return builtInTypes_object;
                }
            }
        }
        return unknownType;
    }

    private static SymbolDefinition ResolveArgumentsNode(SyntaxTreeNode_Rule argumentsNode, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf, SymbolDefinition invokedSymbol, SymbolDefinition memberOf)
    {
        SymbolDefinition result = null;

        invokedSymbol = invokedSymbol ?? invokedLeaf.ResolvedSymbol;

        var argumentListNode = argumentsNode != null && argumentsNode.NumValidNodes >= 2 ? argumentsNode.NodeAt(1) : null;
        if (argumentListNode != null)
            ResolveNode(argumentListNode, scope);

        SymbolReference[] typeArgs = null;
        if (invokedLeaf != null)
        {
            var accessIdentifierNode = invokedLeaf.Parent;
            if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
            {
                var typeArgumentListNode = accessIdentifierNode.NodeAt(2);
                if (typeArgumentListNode != null)
                {
                    var numTypeArguments = typeArgumentListNode.NumValidNodes / 2;
                    typeArgs = new SymbolReference[numTypeArguments];
                    for (int i = 0; i < numTypeArguments; ++i)
                        typeArgs[i] = new SymbolReference(typeArgumentListNode.ChildAt(1 + 2 * i));
                }
            }
        }

        if (invokedSymbol.kind == SymbolKind.MethodGroup)
        {
            result = invokedSymbol.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
            var targetType = invokedSymbol;
            while (targetType != null && !(targetType is TypeDefinitionBase))
                targetType = targetType.parentSymbol;
            while (result == SD_MethodGroup.unresolvedMethodOverload && targetType != null)
            {
                targetType = (targetType as TypeDefinitionBase).BaseType();
                if (targetType != null)
                {
                    var inBase = targetType.FindName(invokedSymbol.name, 0, false);
                    if (inBase != null && inBase.kind == SymbolKind.MethodGroup)
                        result = inBase.ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
                }
            }

            if (result != null && result.kind == SymbolKind.Method && !(result is MethodDefinition))
                result = result as SD_ConstructedReference;

            if (result != null && result.kind != SymbolKind.Error)
            {
                var prevNode = argumentsNode != null ? argumentsNode.Parent.FindPreviousNode() as SyntaxTreeNode_Rule : null;
                var idLeaf = prevNode != null ? prevNode.LeafAt(0) ?? prevNode.NodeAt(0).LeafAt(1) : invokedLeaf;
                if (result.kind == SymbolKind.Error)
                {
                    idLeaf.ResolvedSymbol = invokedSymbol as SD_MethodGroup;
                    idLeaf.m_sSemanticError = result.name;
                }
                else if (idLeaf.ResolvedSymbol != result)
                {
                    idLeaf.ResolvedSymbol = result;
                    idLeaf.m_sSemanticError = null;
                }

                return result;
            }
        }

        if (memberOf != null && !(memberOf is TypeDefinitionBase) &&
            (invokedSymbol.kind == SymbolKind.MethodGroup || invokedSymbol.kind == SymbolKind.Error))
        {
            var memberOfType = memberOf.TypeOf() as TypeDefinitionBase ?? scope.EnclosingType();
            result = scope.ResolveAsExtensionMethod(invokedLeaf, invokedSymbol, memberOfType, argumentListNode, typeArgs, scope);
            if (result != null && result.kind == SymbolKind.Method && !(result is MethodDefinition))
                result = result as SD_ConstructedReference;

            if (result != null)
            {
                if (result.kind == SymbolKind.Error)
                {
                    invokedLeaf.ResolvedSymbol = result;
                    invokedLeaf.m_sSemanticError = result.name;
                }
                else if (invokedLeaf.ResolvedSymbol != result)
                {
                    invokedLeaf.ResolvedSymbol = result;
                    invokedLeaf.m_sSemanticError = null;
                }
            }
        }
        else if (invokedSymbol.kind != SymbolKind.Method && invokedSymbol.kind != SymbolKind.Error)
        {
            var typeOf = invokedSymbol.TypeOf() as TypeDefinitionBase;
            if (typeOf == null || typeOf.kind == SymbolKind.Error)
                return unknownType;

            var returnType = invokedSymbol.kind == SymbolKind.Delegate ? typeOf :
                typeOf.kind == SymbolKind.Delegate ? typeOf.TypeOf() as TypeDefinitionBase : null;
            if (returnType != null)
                return returnType.GetThisInstance();

            var firstLeaf = argumentListNode != null ? argumentListNode.LeafAt(0) : null;
            if (firstLeaf != null)
                firstLeaf.m_sSemanticError = "Cannot invoke symbol";
        }

        return result;
    }

    public static SymbolDefinition ResolveNode(SyntaxTreeNode_Base baseNode, Scope_Base scope = null, SymbolDefinition asMemberOf = null, int numTypeArguments = 0, bool asTypeOnly = false)
    {
        var node = baseNode as SyntaxTreeNode_Rule;

        if (scope == null)
        {
            var scanNode = node;
            while (scanNode != null && scanNode.Parent != null)
            {
                var ruleName = scanNode.RuleName;
                if (ruleName == "type" && scanNode.m_iChildIndex == 2)
                {
                    var nextNode = scanNode.Parent.NodeAt(3);
                    if (nextNode != null &&
                        (nextNode.RuleName == "methodDeclaration" || nextNode.RuleName == "interfaceMethodDeclaration"))
                    {
                        scope = nextNode.scope;
                        break;
                    }
                }

                if (ruleName != "type"
                    && ruleName != "typeName"
                    && ruleName != "namespaceOrTypeName"
                    && ruleName != "typeOrGeneric"
                    && ruleName != "typeArgumentList")
                {
                    break;
                }
                scanNode = scanNode.Parent;
            }
        }

        if (scope == null)
        {
            var scopeNode = Parser_CSharp.EnclosingSemanticNode(baseNode, SemanticFlags.ScopesMask);
            while (scopeNode != null && scopeNode.scope == null && scopeNode.Parent != null)
                scopeNode = Parser_CSharp.EnclosingSemanticNode(scopeNode.Parent, SemanticFlags.ScopesMask);
            if (scopeNode != null)
                scope = scopeNode.scope;
        }

        var leaf = baseNode as SyntaxTreeNode_Leaf;
        if (leaf != null)
        {
            if ((leaf.ResolvedSymbol == null || leaf.m_sSemanticError != null ||
                leaf.ResolvedSymbol.kind == SymbolKind.Method ||
                !leaf.ResolvedSymbol.IsValid()) && leaf.token != null)
            {
                leaf.ResolvedSymbol = null;
                leaf.m_sSemanticError = null;

                switch (leaf.token.tokenKind)
                {
                    case LexerToken.Kind.Identifier:
                        if (asMemberOf != null)
                        {
                            asMemberOf.ResolveMember(leaf, scope, numTypeArguments, asTypeOnly);
                            if (asTypeOnly && leaf.ResolvedSymbol == null)
                            {
                                asMemberOf.ResolveMember(leaf, scope, numTypeArguments, false);
                                if (leaf.ResolvedSymbol != null && leaf.ResolvedSymbol.kind != SymbolKind.Error)
                                {
                                    leaf.m_sSemanticError = "Type expected!";
                                }
                            }
                        }
                        else if (scope != null)
                        {
                            if (leaf.token.text == "global")
                            {
                                var nextLeaf = leaf.FindNextLeaf();
                                if (nextLeaf != null && nextLeaf.IsLit("::"))
                                {
                                    var assembly = scope.GetAssembly();
                                    if (assembly != null)
                                    {
                                        leaf.ResolvedSymbol = scope.GetAssembly().GlobalNamespace;
                                        return leaf.ResolvedSymbol;
                                    }
                                }
                            }
                            scope.Resolve(leaf, numTypeArguments, asTypeOnly);
                            if (asTypeOnly && leaf.ResolvedSymbol == null)
                            {
                                scope.Resolve(leaf, numTypeArguments, false);
                                if (leaf.ResolvedSymbol != null && leaf.ResolvedSymbol.kind != SymbolKind.Error)
                                {
                                    leaf.m_sSemanticError = "Type expected!";
                                }
                            }
                        }
                        if (leaf.ResolvedSymbol == null)
                        {
                            if (asMemberOf != null)
                                asMemberOf.ResolveMember(leaf, scope, -1, asTypeOnly);
                            else if (scope != null)
                                scope.Resolve(leaf, -1, asTypeOnly);
                        }
                        if (leaf.ResolvedSymbol != null &&
                            leaf.ResolvedSymbol.NumTypeParameters != numTypeArguments &&
                            leaf.ResolvedSymbol.kind != SymbolKind.Error)
                        {
                            if (leaf.ResolvedSymbol is TypeDefinitionBase)
                            {
                                leaf.m_sSemanticError = string.Format("Type '{0}' does not take {1} type argument{2}",
                                    leaf.ResolvedSymbol.GetName(), numTypeArguments, numTypeArguments == 1 ? "" : "s");
                            }
                            else if (numTypeArguments > 0 &&
                                (leaf.ResolvedSymbol.kind == SymbolKind.Method))
                            {
                                leaf.m_sSemanticError = string.Format("Method '{0}' does not take {1} type argument{2}",
                                    leaf.token.text, numTypeArguments, numTypeArguments == 1 ? "" : "s");
                            }
                        }
                        break;

                    case LexerToken.Kind.Keyword:
                        if (leaf.token.text == "this" || leaf.token.text == "base")
                        {
                            var scopeNode = Parser_CSharp.EnclosingScopeNode(leaf.Parent,
                                SemanticFlags.MethodBodyScope,
                                SemanticFlags.AccessorBodyScope);//,
                            if (scopeNode == null)
                            {
                                if (leaf.m_iChildIndex == 1 && leaf.Parent.RuleName == "constructorInitializer")
                                {
                                    var bodyScope = scope.parentScope.parentScope as Scope_Body;
                                    if (bodyScope == null)
                                        break;

                                    asMemberOf = bodyScope.definition;
                                    if (asMemberOf.kind != SymbolKind.Class && asMemberOf.kind != SymbolKind.Struct)
                                        break;

                                    if (leaf.token.text == "base")
                                    {
                                        if (asMemberOf.kind == SymbolKind.Struct)
                                            break;
                                        asMemberOf = ((TypeDefinitionBase)asMemberOf).BaseType();
                                    }
                                    leaf.ResolvedSymbol = ResolveNodeAsConstructor(leaf.Parent.NodeAt(2), scope, asMemberOf);
                                }
                                break;
                            }

                            var memberScope = scopeNode.scope as Scope_Body;
                            if (memberScope != null && memberScope.definition.IsStatic)
                            {
                                if (leaf.token.text == "base")
                                    leaf.ResolvedSymbol = baseInStaticMember;
                                else
                                    leaf.ResolvedSymbol = thisInStaticMember;
                                break;
                            }

                            scopeNode = Parser_CSharp.EnclosingScopeNode(scopeNode, SemanticFlags.TypeDeclarationScope);
                            if (scopeNode == null)
                            {
                                leaf.ResolvedSymbol = unknownSymbol;
                                break;
                            }

                            var thisType = ((Scope_SymbolDeclaration)scopeNode.scope).declaration.definition as TypeDefinitionBase;
                            if (thisType != null && leaf.token.text == "base")
                                thisType = thisType.BaseType();
                            if (thisType != null && (thisType.kind == SymbolKind.Struct || thisType.kind == SymbolKind.Class))
                                leaf.ResolvedSymbol = thisType.GetThisInstance();
                            else
                                leaf.ResolvedSymbol = unknownSymbol;
                            break;
                        }
                        else
                        {
                            TypeDefinitionBase type;
                            if (builtInTypes.TryGetValue(leaf.token.text, out type))
                                leaf.ResolvedSymbol = type;
                        }
                        break;

                    case LexerToken.Kind.CharLiteral:
                        leaf.ResolvedSymbol = builtInTypes_char.GetThisInstance();
                        break;

                    case LexerToken.Kind.IntegerLiteral:
                        var endsWith = leaf.token.text[leaf.token.text.Length - 1];
                        var unsignedDecimal = endsWith == 'u' || endsWith == 'U';
                        var longDecimal = endsWith == 'l' || endsWith == 'L';
                        if (unsignedDecimal)
                        {
                            endsWith = leaf.token.text[leaf.token.text.Length - 2];
                            longDecimal = endsWith == 'l' || endsWith == 'L';
                        }
                        else if (longDecimal)
                        {
                            endsWith = leaf.token.text[leaf.token.text.Length - 2];
                            unsignedDecimal = endsWith == 'u' || endsWith == 'U';
                        }
                        leaf.ResolvedSymbol =
                            longDecimal ? (unsignedDecimal ? builtInTypes_ulong.GetThisInstance() : builtInTypes_long.GetThisInstance())
                            : unsignedDecimal ? builtInTypes_uint.GetThisInstance() : builtInTypes_int.GetThisInstance();
                        break;

                    case LexerToken.Kind.RealLiteral:
                        endsWith = leaf.token.text[leaf.token.text.Length - 1];
                        leaf.ResolvedSymbol =
                            endsWith == 'f' || endsWith == 'F' ? builtInTypes_float.GetThisInstance() :
                            endsWith == 'm' || endsWith == 'M' ? builtInTypes_decimal.GetThisInstance() :
                            builtInTypes_double.GetThisInstance();
                        break;

                    case LexerToken.Kind.StringLiteral:
                    case LexerToken.Kind.VerbatimStringBegin:
                    case LexerToken.Kind.VerbatimStringLiteral:
                        leaf.ResolvedSymbol = builtInTypes_string.GetThisInstance();
                        break;

                    case LexerToken.Kind.BuiltInLiteral:
                        leaf.ResolvedSymbol = leaf.token.text == "null" ? nullLiteral : builtInTypes_bool.GetThisInstance();
                        break;

                    case LexerToken.Kind.Missing:
                        return null;

                    case LexerToken.Kind.ContextualKeyword:
                        return null;

                    case LexerToken.Kind.Punctuator:
                        return null;

                    default:
                        Debug.LogWarning(leaf.ToString());
                        return null;
                }

                if (leaf.ResolvedSymbol == null)
                    leaf.ResolvedSymbol = unknownSymbol;
                if (leaf.m_sSemanticError == null && leaf.ResolvedSymbol.kind == SymbolKind.Error)
                    leaf.m_sSemanticError = leaf.ResolvedSymbol.name;
            }
            return leaf.ResolvedSymbol;
        }

        if (node == null || node.NumValidNodes == 0 || node.m_bMissing)
            return null;

        int rank;
        SymbolDefinition part = null, dummy = null;
        switch (node.RuleName)
        {
            case "localVariableType":
                if (node.NumValidNodes == 1)
                    return ResolveNode(node.ChildAt(0), scope, asMemberOf);
                break;

            case "GET":
            case "SET":
            case "ADD":
            case "REMOVE":
                SymbolDeclaration declaration = null;
                for (var tempNode = node; declaration == null && tempNode != null; tempNode = tempNode.Parent)
                    declaration = tempNode.declaration;
                if (declaration == null)
                    return node.ChildAt(0).ResolvedSymbol = unknownSymbol;
                return node.ChildAt(0).ResolvedSymbol = declaration.definition;

            case "YIELD":
            case "FROM":
            case "SELECT":
            case "WHERE":
            case "GROUP":
            case "INTO":
            case "ORDERBY":
            case "JOIN":
            case "LET":
            case "ON":
            case "EQUALS":
            case "BY":
            case "ASCENDING":
            case "DESCENDING":
            case "ATTRIBUTETARGET":
                node.ChildAt(0).ResolvedSymbol = contextualKeyword;
                return contextualKeyword;

            case "memberName":
                declaration = null;
                while (declaration == null && node != null)
                {
                    declaration = node.declaration;
                    node = node.Parent;
                }
                if (declaration == null)
                    return unknownSymbol;
                return declaration.definition;

            case "VAR":
                SyntaxTreeNode_Rule varDeclsNode = null;
                if (node.Parent.Parent.RuleName == "foreachStatement" && node.Parent.Parent.NumValidNodes >= 6)
                {
                    varDeclsNode = node.Parent.Parent.NodeAt(5);
                    if (varDeclsNode != null && varDeclsNode.NumValidNodes == 1)
                    {
                        var elementType = EnumerableElementType(varDeclsNode);
                        node.ChildAt(0).ResolvedSymbol = elementType;
                    }
                }
                else if (node.Parent.Parent.NumValidNodes >= 2)
                {
                    varDeclsNode = node.Parent.Parent.NodeAt(1);
                    if (varDeclsNode != null && varDeclsNode.NumValidNodes == 1)
                    {
                        var declNode = varDeclsNode.NodeAt(0);
                        if (declNode != null && declNode.NumValidNodes == 3)
                        {
                            var initExpr = ResolveNode(declNode.ChildAt(2));
                            var varLeaf = node.ChildAt(0);
                            varLeaf.m_sSemanticError = null;
                            if (initExpr != null && initExpr.kind != SymbolKind.Error)
                                varLeaf.ResolvedSymbol = initExpr.TypeOf();
                            else
                                varLeaf.ResolvedSymbol = unknownType;
                        }
                        else
                            node.ChildAt(0).ResolvedSymbol = unknownType;
                    }
                }
                else
                    node.ChildAt(0).ResolvedSymbol = unknownType;
                return node.ChildAt(0).ResolvedSymbol;

            case "type":
            case "type2":
                var resolvedTypeNode = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments, true);
                var typeNodeType = resolvedTypeNode as TypeDefinitionBase;
                if (typeNodeType != null)
                {
                    if (node.NumValidNodes > 1)
                    {
                        var nullableShorthand = node.LeafAt(1);
                        if (nullableShorthand != null && nullableShorthand.token.text == "?")
                        {
                            typeNodeType = typeNodeType.MakeNullableType();
                        }

                        var rankNode = node.NodeAt(-1);
                        if (rankNode != null)
                        {
                            typeNodeType = typeNodeType.MakeArrayType(rankNode.NumValidNodes - 1);
                        }
                    }
                    return typeNodeType;
                }
                else if (resolvedTypeNode != null && resolvedTypeNode.kind != SymbolKind.Error)
                {
                    var firstLeaf = node.LeafAt(0) ?? node.NodeAt(0).GetFirstLeaf();
                    if (firstLeaf != null)
                        firstLeaf.m_sSemanticError = "Type expected";
                }
                break;

            case "attribute":
                var attributeTypeName = ResolveNode(node.ChildAt(0), scope);
                if (node.NumValidNodes == 2)
                    ResolveNode(node.ChildAt(1), null);
                return attributeTypeName;

            case "integralType":
            case "simpleType":
            case "numericType":
            case "floatingPointType":
            case "predefinedType":
            case "typeName":
            case "exceptionClassType":
                var resolvedType = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArguments, true);
                if (resolvedType != null && resolvedType.kind != SymbolKind.Error && !(resolvedType is TypeDefinitionBase))
                    node.GetFirstLeaf().m_sSemanticError = "Type expected";
                return resolvedType;

            case "globalNamespace":
                return ResolveNode(node.ChildAt(0), scope, null, 0);

            case "nonArrayType":
                var nonArrayTypeSymbol = ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true);
                var nonArrayType = nonArrayTypeSymbol as TypeDefinitionBase;
                if (nonArrayType != null && node.NumValidNodes == 2)
                    return nonArrayType.MakeNullableType();
                return nonArrayType;

            case "typeParameter":
                return ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true) as TypeDefinitionBase;

            case "typeVariableName":
                return ResolveNode(node.ChildAt(0), scope) as SD_Type_Parameter;

            case "typeOrGeneric":
                if (asMemberOf == null && node.m_iChildIndex > 0)
                    asMemberOf = ResolveNode(node.Parent.ChildAt(node.m_iChildIndex - 2), scope, null, 0, true);
                if (node.NumValidNodes >= 2)
                {
                    var typeArgsListNode = node.NodeAt(1);
                    if (typeArgsListNode != null && typeArgsListNode.NumValidNodes > 0)
                    {
                        bool isUnboundType = typeArgsListNode.RuleName == "unboundTypeRank";
                        var numTypeArgs = isUnboundType ? typeArgsListNode.NumValidNodes - 1 : typeArgsListNode.NumValidNodes / 2;
                        var typeDefinition = ResolveNode(node.ChildAt(0), scope, asMemberOf, numTypeArgs, true) as SD_Type;
                        if (typeDefinition == null)
                            return node.ChildAt(0).ResolvedSymbol;

                        if (!isUnboundType)
                        {
                            var typeArgs = new SymbolReference[numTypeArgs];
                            for (var i = 0; i < numTypeArgs; ++i)
                                typeArgs[i] = new SymbolReference(typeArgsListNode.ChildAt(1 + 2 * i));
                            if (typeDefinition.typeParameters != null)
                            {
                                var constructedType = typeDefinition.ConstructType(typeArgs);
                                node.ChildAt(0).ResolvedSymbol = constructedType;
                                return constructedType;
                            }
                        }

                        return typeDefinition;
                    }
                }
                else if (scope is Scope_Attributes && node.m_iChildIndex == node.Parent.NumValidNodes - 1 && node.Parent.Parent.Parent.RuleName == "attribute")
                {
                    var lastLeaf = node.LeafAt(0);
                    if (asMemberOf != null)
                        asMemberOf.ResolveAttributeMember(lastLeaf, scope);
                    else
                        scope.ResolveAttribute(lastLeaf);

                    if (lastLeaf.ResolvedSymbol == null)
                        lastLeaf.ResolvedSymbol = unknownSymbol;
                    return lastLeaf.ResolvedSymbol;
                }
                return ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true);

            case "namespaceName":
                var resolvedSymbol = ResolveNode(node.ChildAt(0), scope, asMemberOf, 0, true);
                if (resolvedSymbol != null && resolvedSymbol.kind != SymbolKind.Error && !(resolvedSymbol is SD_NameSpace))
                    node.ChildAt(0).m_sSemanticError = "Namespace name expected";
                return resolvedSymbol;

            case "namespaceOrTypeName":
                part = ResolveNode(node.ChildAt(0), scope, null, node.NumValidNodes == 1 ? numTypeArguments : 0, true);
                for (var i = 2; i < node.NumValidNodes; i += 2)
                    part = ResolveNode(node.ChildAt(i), scope, part, i == node.NumValidNodes - 1 ? numTypeArguments : 0, true);
                return part;

            case "Directive_UsingAlias":
                return ResolveNode(node.ChildAt(0), scope);

            case "qualifiedIdentifier":
                part = ResolveNode(node.ChildAt(0), scope) as SD_NameSpace;
                for (var i = 2; part != null && i < node.NumValidNodes; i += 2)
                {
                    part = ResolveNode(node.ChildAt(i), scope, part);
                    var idNode = node.NodeAt(i);
                    if (idNode != null && idNode.NumValidNodes == 1)
                        idNode.ChildAt(0).ResolvedSymbol = part;
                }
                return part;

            case "destructorDeclarator":
                return builtInTypes_void;

            case "memberInitializer":
                ResolveNode(node.ChildAt(0), scope);
                if (node.NumValidNodes == 3)
                    ResolveNode(node.ChildAt(2), scope);
                return null;

            case "primaryExpression":
                var invokeTarget = part;
                SyntaxTreeNode_Leaf invokeTargetLeaf = null;
                for (var i = 0; i < node.NumValidNodes; ++i)
                {
                    var child = node.ChildAt(i);
                    var childAsLeaf = child as SyntaxTreeNode_Leaf;
                    if (childAsLeaf != null && childAsLeaf.m_bMissing)
                        return part;

                    var methodNameNode = child as SyntaxTreeNode_Rule;
                    SymbolDefinition nextPart = null;

                    if (i == 0 && childAsLeaf != null && childAsLeaf.token.text == "new")
                    {
                        methodNameNode = node.NodeAt(1);
                        if (methodNameNode != null && methodNameNode.NumValidNodes > 0)
                        {
                            var nonArrayTypeNode = methodNameNode.RuleName == "nonArrayType" ? methodNameNode : null;
                            if (nonArrayTypeNode != null)
                            {
                                asMemberOf = ResolveNode(nonArrayTypeNode, scope);
                                var node3 = node.NodeAt(2);
                                if (node3 != null && node3.RuleName == "objectCreationExpression")
                                {
                                    i += 2;
                                    nextPart = ResolveNodeAsConstructor(node3, scope, asMemberOf);
                                    if (nextPart != null && nextPart.kind == SymbolKind.Constructor)
                                    {
                                        var asMemberOfAsConstructedType = asMemberOf as SD_Type_Constructed;
                                        if (asMemberOfAsConstructedType != null)
                                            nextPart = asMemberOfAsConstructedType.GetConstructedMember(nextPart);
                                    }
                                }
                                else if (node3 != null)
                                {
                                    i += 2;
                                    nextPart = ResolveNode(node.ChildAt(i), scope, asMemberOf);
                                }
                            }
                            else
                            {
                                nextPart = ResolveNode(methodNameNode, scope);
                            }
                        }
                    }
                    else
                    {
                        var primaryExpressionPartNode = i != 0 ? child as SyntaxTreeNode_Rule : null;
                        var argumentsNode = primaryExpressionPartNode != null ? primaryExpressionPartNode.NodeAt(0) : null;
                        if (argumentsNode != null && argumentsNode.RuleName == "arguments")
                        {
                            nextPart = ResolveArgumentsNode(argumentsNode, scope, invokeTargetLeaf, part, asMemberOf);

                            var parameters = nextPart != null ? nextPart.GetParameters() : null;
                            if (parameters != null)
                            {
                                var argumentListNode2 = argumentsNode != null && argumentsNode.NumValidNodes >= 2 ? argumentsNode.NodeAt(1) : null;
                                if (argumentListNode2 != null)
                                {
                                    for (var j = 0; j < argumentListNode2.NumValidNodes; j += 2)
                                    {
                                        var argumentNode = argumentListNode2.NodeAt(j);
                                        if (argumentNode == null)
                                            continue;

                                        var lambdaExpressionBodyNode = argumentNode.FindChildByName(lambdaBodyRulePath);
                                        if (lambdaExpressionBodyNode != null)
                                            ResolveNode(lambdaExpressionBodyNode);
                                    }
                                }
                            }
                        }
                        else
                        {
                            nextPart = ResolveNode(child, scope, part);
                        }
                    }

                    asMemberOf = part;

                    if (nextPart != null && nextPart.kind != SymbolKind.Error)
                    {
                        SymbolDefinition method = nextPart.kind == SymbolKind.Method || nextPart.kind == SymbolKind.Constructor ? nextPart : null;
                        if (nextPart.kind == SymbolKind.MethodGroup)
                        {
                            if (methodNameNode.NumValidNodes == 2 && !(nextPart is SD_ConstructedMethodGroup))
                            {
                                nextPart = ResolveNode(methodNameNode.NodeAt(1), scope, nextPart);
                            }
                        }

                        if (method != null)
                        {
                            if (methodNameNode != null)
                            {
                                if (methodNameNode.RuleName == "primaryExpressionStart")
                                {
                                    var methodNameLeaf = methodNameNode.LeafAt(methodNameNode.NumValidNodes < 3 ? 0 : 2);
                                    if (methodNameLeaf != null)
                                        methodNameLeaf.ResolvedSymbol = nextPart;
                                }
                                else if (methodNameNode.RuleName == "primaryExpressionPart")
                                {
                                    var accessIdentifierNode = methodNameNode.NodeAt(0);
                                    if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
                                    {
                                        var methodNameLeaf = accessIdentifierNode.LeafAt(1);
                                        if (methodNameLeaf != null)
                                            methodNameLeaf.ResolvedSymbol = nextPart;
                                    }
                                }
                            }
                            else
                            {
                                node.ChildAt(i).ResolvedSymbol = method;
                            }
                        }
                    }

                    var childNode = child as SyntaxTreeNode_Rule;
                    if (childNode != null)
                        childNode = childNode.NodeAt(0);
                    if (childNode != null)
                    {
                        if (nextPart != null && invokeTarget != null && !(invokeTarget is TypeDefinitionBase) &&
                            (nextPart is TypeDefinitionBase || nextPart.IsStatic))
                        {
                            switch (invokeTarget.kind)
                            {
                                case SymbolKind.ConstantField:
                                case SymbolKind.Field:
                                case SymbolKind.Property:
                                case SymbolKind.Indexer:
                                case SymbolKind.Event:
                                case SymbolKind.LocalConstant:
                                case SymbolKind.Variable:
                                case SymbolKind.ForEachVariable:
                                case SymbolKind.FromClauseVariable:
                                case SymbolKind.Parameter:
                                case SymbolKind.CatchParameter:
                                case SymbolKind.Instance:
                                    var parentType = nextPart.parentSymbol;
                                    while (parentType != null && !(parentType is TypeDefinitionBase))
                                        parentType = parentType.parentSymbol;
                                    if (parentType != null && parentType.NumTypeParameters == 0 &&
                                        invokeTarget.NumTypeParameters == 0 && invokeTarget.name == parentType.name)
                                    {
                                        invokeTargetLeaf.ResolvedSymbol = parentType;
                                    }
                                    break;
                            }
                        }
                    }
                    part = nextPart;
                    if (part == null)
                        break;

                    if (part.kind == SymbolKind.Method)
                    {
                        var currentNode = child as SyntaxTreeNode_Rule;
                        if (currentNode != null)
                            currentNode = currentNode.RuleName == "primaryExpressionPart" ? currentNode.NodeAt(0) : null;
                        if (currentNode == null || currentNode.RuleName != "arguments")
                            part = part.parentSymbol;
                    }

                    if (part.kind == SymbolKind.Method)
                    {
                        var returnType = (part = part.TypeOf()) as TypeDefinitionBase;
                        if (returnType != null)
                            part = returnType.GetThisInstance();
                    }
                    else if (part.kind == SymbolKind.Constructor)
                    {
                        part = ((TypeDefinitionBase)part.parentSymbol).GetThisInstance();
                    }

                    if (part == null)
                        break;

                    if (part.kind != SymbolKind.MethodGroup)
                    {
                        invokeTarget = part;
                    }

                    var partNode = child as SyntaxTreeNode_Rule;
                    if (partNode != null)
                    {
                        if (partNode.RuleName == "primaryExpressionPart")
                        {
                            var accessIdentifierNode = partNode.NodeAt(0);
                            if (accessIdentifierNode != null && accessIdentifierNode.RuleName == "accessIdentifier")
                            {
                                invokeTargetLeaf = accessIdentifierNode.LeafAt(1);
                            }
                            else
                            {
                                invokeTargetLeaf = null;
                            }
                        }
                        else if (partNode.RuleName == "primaryExpressionStart")
                        {
                            var identifierLeaf = partNode.LeafAt(0);
                            if (identifierLeaf != null && identifierLeaf.token.tokenKind == LexerToken.Kind.Identifier)
                                invokeTargetLeaf = partNode.LeafAt(partNode.NumValidNodes == 3 ? 2 : 0);
                        }
                    }
                }
                return part ?? unknownSymbol;

            case "primaryExpressionStart":
                if (node.NumValidNodes == 1)
                    return ResolveNode(node.ChildAt(0), scope, null);
                if (node.NumValidNodes == 2)
                {
                    var typeArgsNode = node.NodeAt(1);
                    if (typeArgsNode != null && typeArgsNode.RuleName == "typeArgumentList")
                        numTypeArguments = typeArgsNode.NumValidNodes / 2;
                    asMemberOf = ResolveNode(node.ChildAt(0), scope, null, numTypeArguments);
                    return ResolveNode(typeArgsNode, scope, asMemberOf);
                }
                if (node.NumValidNodes == 3)
                {
                    part = ResolveNode(node.ChildAt(0), scope, null);
                    return ResolveNode(node.ChildAt(2), scope, part);
                }
                break;

            case "primaryExpressionPart":
                if (asMemberOf == null)
                {
                    asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
                    if (asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
                        asMemberOf = asMemberOf.TypeOf();
                }
                if (asMemberOf != null)
                    return ResolveNode(node.ChildAt(0), scope, asMemberOf);
                break;

            case "brackets":
                if (asMemberOf == null)
                    asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
                if (asMemberOf != null)
                {
                    var arrayType = asMemberOf.TypeOf() as SD_Type_Array;
                    if (arrayType != null && arrayType.elementType != null)
                    {
                        return arrayType.elementType.GetThisInstance();
                    }
                    if (node.NumValidNodes == 3)
                    {
                        var expressionListNode = node.NodeAt(1);
                        if (expressionListNode != null && expressionListNode.NumValidNodes >= 1)
                        {
                            var argumentTypes = new TypeDefinitionBase[(expressionListNode.NumValidNodes + 1) / 2];
                            for (var i = 0; i < argumentTypes.Length; ++i)
                            {
                                var expression = ResolveNode(expressionListNode.ChildAt(i * 2), scope);
                                if (expression == null)
                                    goto default;
                                argumentTypes[i] = expression.TypeOf() as TypeDefinitionBase;
                            }
                            var typeOf = asMemberOf.TypeOf() as TypeDefinitionBase;
                            var indexer = typeOf == null ? null : typeOf.GetIndexer(argumentTypes);
                            if (indexer != null)
                            {
                                typeOf = indexer.TypeOf() as TypeDefinitionBase;
                                return typeOf == null ? null : typeOf.GetThisInstance();
                            }
                            else
                            {
                                return unknownSymbol;
                            }
                        }
                    }
                }
                break;

            case "accessIdentifier":
                if (asMemberOf == null)
                {
                    asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
                    if (asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
                        asMemberOf = asMemberOf.TypeOf();
                }
                if (node.NumValidNodes == 2)
                {
                    var node1 = node.ChildAt(1);
                    if (!node1.m_bMissing)
                        return ResolveNode(node.ChildAt(1), scope, asMemberOf);
                }
                else if (node.NumValidNodes == 3)
                {
                    var typeArgsNode = node.NodeAt(2);
                    if (typeArgsNode != null && typeArgsNode.RuleName == "typeArgumentList")
                        numTypeArguments = typeArgsNode.NumValidNodes / 2;
                    asMemberOf = ResolveNode(node.ChildAt(1), scope, asMemberOf, numTypeArguments);
                    return ResolveNode(typeArgsNode, scope, asMemberOf);
                }
                return asMemberOf;

            case "typeArgumentList":
                if (asMemberOf == null)
                {
                    asMemberOf = ResolveNode(node.FindPreviousNode(), scope);
                }
                numTypeArguments = node.NumValidNodes / 2;
                var genericMethodGroup = asMemberOf as SD_MethodGroup;
                if (genericMethodGroup != null)
                {
                    var typeArgs = new SymbolReference[numTypeArguments];
                    for (var i = 0; i < numTypeArguments; ++i)
                        typeArgs[i] = new SymbolReference(node.ChildAt(2 * i + 1));
                    return genericMethodGroup.ConstructMethodGroup(typeArgs);
                }
                else
                {
                    var genericType = asMemberOf as SD_Type;
                    if (genericType != null)
                    {
                        var typeArgs = new SymbolReference[numTypeArguments];
                        for (var i = 0; i < numTypeArguments; ++i)
                            typeArgs[i] = new SymbolReference(node.ChildAt(2 * i + 1));
                        var constructedType = genericType.ConstructType(typeArgs);
                        if (constructedType != null)
                        {
                            var prevNode = node.FindPreviousNode() as SyntaxTreeNode_Leaf;
                            if (prevNode != null)
                                prevNode.ResolvedSymbol = constructedType;
                            return constructedType;
                        }
                    }
                }
                return asMemberOf;

            case "attributeArguments":
                if (asMemberOf == null)
                {
                    var prevNode = node.FindPreviousNode();
                    asMemberOf = ResolveNode(prevNode, scope);
                }
                var attributeArgumentListNode = node.NumValidNodes >= 2 ? node.NodeAt(1) : null;
                if (attributeArgumentListNode != null)
                    ResolveNode(attributeArgumentListNode, scope, asMemberOf);
                return resolvedChildren;

            case "arguments":
                if (asMemberOf == null)
                {
                    var prevBaseNode = node.FindPreviousNode();
                    asMemberOf = ResolveNode(prevBaseNode, scope);
                    if (asMemberOf == null)
                    {
                        return null;
                    }
                }

                var argumentListNode = node.NumValidNodes >= 2 ? node.NodeAt(1) : null;
                if (argumentListNode != null)
                    ResolveNode(argumentListNode, scope);

                if (node.Parent.RuleName == "attribute" || node.Parent.RuleName == "constructorInitializer")
                    return unknownSymbol;

                var nodeLeftOfArguments = node.FindPreviousNode() as SyntaxTreeNode_Rule;
                var idLeaf = nodeLeftOfArguments.LeafAt(0) ?? nodeLeftOfArguments.NodeAt(0).LeafAt(1);

                var methodGroup = asMemberOf as SD_MethodGroup;
                if (methodGroup != null)
                {
                    asMemberOf = methodGroup.ResolveMethodOverloads(argumentListNode, null, scope, idLeaf);
                    SymbolDefinition method = asMemberOf as MethodDefinition;
                    if (method == null && asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
                        method = asMemberOf as SD_ConstructedReference;
                    if (method != null)
                    {
                        if (method.kind == SymbolKind.Error)
                        {
                            idLeaf.ResolvedSymbol = methodGroup;
                            idLeaf.m_sSemanticError = method.name;
                        }
                        else if (idLeaf.ResolvedSymbol != method)
                        {
                            idLeaf.ResolvedSymbol = method;
                        }
                        return method;
                    }
                }
                else if (asMemberOf.kind == SymbolKind.MethodGroup)
                {
                    var constructedMethodGroup = asMemberOf as SD_ConstructedReference;
                    if (constructedMethodGroup != null)
                        asMemberOf = constructedMethodGroup.ResolveMethodOverloads(argumentListNode, null, scope, idLeaf);
                    SymbolDefinition method = asMemberOf as MethodDefinition;
                    if (method == null && asMemberOf != null && asMemberOf.kind == SymbolKind.Method)
                        method = asMemberOf as SD_ConstructedReference;
                    if (method != null)
                    {
                        if (method.kind == SymbolKind.Error)
                        {
                            idLeaf.ResolvedSymbol = methodGroup;
                            idLeaf.m_sSemanticError = method.name;
                        }
                        else if (idLeaf.ResolvedSymbol != method)
                        {
                            idLeaf.ResolvedSymbol = method;
                        }

                        return method;
                    }
                }
                else if (asMemberOf.kind != SymbolKind.Method && asMemberOf.kind != SymbolKind.Error)
                {
                    var typeOf = asMemberOf.TypeOf() as TypeDefinitionBase;
                    if (typeOf == null || typeOf.kind == SymbolKind.Error)
                        return unknownType;

                    var returnType = asMemberOf.kind == SymbolKind.Delegate ? typeOf :
                        typeOf.kind == SymbolKind.Delegate ? typeOf.TypeOf() as TypeDefinitionBase : null;
                    if (returnType != null)
                        return returnType.GetThisInstance();

                    node.LeafAt(0).m_sSemanticError = "Cannot invoke symbol";
                }

                return asMemberOf;

            case "argument":
                if (node.NumValidNodes >= 1)
                {
                    if (node.NumValidNodes == 1)
                        return ResolveNode(node.ChildAt(0), scope);
                    else
                        ResolveNode(node.ChildAt(0), scope);
                }
                if (node.NumValidNodes == 3)
                {
                    return ResolveNode(node.ChildAt(2), scope);
                }
                return resolvedChildren;

            case "attributeArgument":
                if (node.NumValidNodes >= 1)
                {
                    if (node.NumValidNodes == 1)
                        return ResolveNode(node.ChildAt(0), scope);
                    else
                        ResolveNode(node.ChildAt(0), scope, asMemberOf);
                }
                if (node.NumValidNodes == 3)
                {
                    return ResolveNode(node.ChildAt(2), scope);
                }
                return resolvedChildren;

            case "argumentList":
                for (var i = 0; i < node.NumValidNodes; i += 2)
                    dummy = ResolveNode(node.ChildAt(i), scope);
                return dummy;

            case "attributeArgumentList":
                for (var i = 0; i < node.NumValidNodes; i += 2)
                    dummy = ResolveNode(node.ChildAt(i), scope, asMemberOf);
                return resolvedChildren;

            case "argumentValue":
                return ResolveNode(node.ChildAt(-1), scope);

            case "argumentName":
                var parameterNameLeaf = node.LeafAt(0);
                if (parameterNameLeaf == null)
                    return unknownSymbol;
                var arguments = node.Parent.Parent.Parent;
                var invokableNode = arguments.FindPreviousNode();
                var invokableSymbol = ResolveNode(invokableNode);
                if (invokableSymbol.kind != SymbolKind.MethodGroup)
                    invokableSymbol = invokableSymbol.parentSymbol;
                methodGroup = invokableSymbol as SD_MethodGroup;
                if (methodGroup == null)
                    return parameterNameLeaf.ResolvedSymbol = unknownSymbol;
                return methodGroup.ResolveParameterName(parameterNameLeaf);

            case "attributeMemberName":
                var asType = asMemberOf as TypeDefinitionBase;
                if (asType != null)
                    return ResolveNode(node.ChildAt(0), scope, asType.GetThisInstance());
                return unknownSymbol;

            case "castExpression":
                if (node.NumValidNodes == 4)
                {
                    var target = ResolveNode(node.ChildAt(3), scope);
                    if (target is TypeDefinitionBase || target != null && target.kind == SymbolKind.Namespace)
                    {
                        ResolveNode(node.ChildAt(1), scope);
                        return target;
                    }
                }
                var castType = ResolveNode(node.ChildAt(1), scope) as TypeDefinitionBase;
                if (castType != null)
                    return castType.GetThisInstance();
                break;

            case "typeofExpression":
                if (node.NumValidNodes >= 3)
                    ResolveNode(node.ChildAt(2), scope);
                return ((TypeDefinitionBase)ReflectedTypeReference.ForType(typeof(Type)).Definition).GetThisInstance();

            case "defaultValueExpression":
                if (node.NumValidNodes >= 3)
                {
                    var typeNode = ResolveNode(node.ChildAt(2), scope) as TypeDefinitionBase;
                    if (typeNode != null)
                        return typeNode.GetThisInstance();
                }
                break;

            case "sizeofExpression":
                if (node.NumValidNodes >= 3)
                    ResolveNode(node.ChildAt(2), scope);
                return builtInTypes_int.GetThisInstance();

            case "checkedExpression":
            case "uncheckedExpression":
                if (node.NumValidNodes >= 3)
                    return ResolveNode(node.ChildAt(2), scope);
                return unknownSymbol;

            case "assignment":
                if (node.NumValidNodes >= 3)
                    ResolveNode(node.ChildAt(2), scope);
                return ResolveNode(node.ChildAt(0), scope);

            case "localVariableInitializer":
            case "variableReference":
            case "expression":
            case "constantExpression":
            case "nonAssignmentExpression":
                return ResolveNode(node.ChildAt(0), scope);

            case "parenExpression":
                return ResolveNode(node.ChildAt(1), scope);

            case "nullCoalescingExpression":
                for (var i = 2; i < node.NumValidNodes; i += 2)
                    ResolveNode(node.ChildAt(i), scope);
                var lhs = ResolveNode(node.ChildAt(0), scope);
                if (node.NumValidNodes >= 2 && lhs != null && (lhs.TypeOf() ?? unknownType).GetGenericSymbol() == builtInTypes_Nullable)
                {
                    var constructedType = lhs.TypeOf() as SD_Type_Constructed;
                    if (constructedType != null)
                    {
                        var nullableType = constructedType.typeArguments[0].Definition as TypeDefinitionBase;
                        if (nullableType != null)
                            return nullableType.GetThisInstance();
                    }
                }
                return lhs;

            case "conditionalExpression":
                if (node.NumValidNodes >= 3)
                {
                    ResolveNode(node.ChildAt(0), scope);
                    var typeRight = nullLiteral;
                    if (node.NumValidNodes == 5)
                        typeRight = ResolveNode(node.ChildAt(4), scope);
                    var typeLeft = ResolveNode(node.ChildAt(2), scope); // HACK
                    return typeLeft != nullLiteral ? typeLeft : typeRight;
                }
                else
                    return ResolveNode(node.ChildAt(0), scope, asMemberOf);

            case "unaryExpression":
                if (node.NumValidNodes == 1)
                    return ResolveNode(node.ChildAt(0), scope, null);
                if (node.ChildAt(0) is SyntaxTreeNode_Rule)
                    return ResolveNode(node.ChildAt(0), scope, null);
                return ResolveNode(node.ChildAt(1), scope, null);

            case "preIncrementExpression":
            case "preDecrementExpression":
                if (node.NumValidNodes == 2)
                    return ResolveNode(node.ChildAt(1), scope, null);
                return builtInTypes_int.GetThisInstance();

            case "inclusiveOrExpression":
            case "exclusiveOrExpression":
            case "andExpression":
            case "shiftExpression":
            case "additiveExpression":
            case "multiplicativeExpression":
                for (var i = 2; i < node.NumValidNodes; i += 2)
                    ResolveNode(node.ChildAt(i), scope);
                return ResolveNode(node.ChildAt(0), scope); // HACK

            case "arrayCreationExpression":
                if (asMemberOf == null)
                    asMemberOf = ResolveNode(node.FindPreviousNode());
                var resultType = asMemberOf as TypeDefinitionBase;
                if (resultType == null)
                    return unknownType.MakeArrayType(1);

                var rankSpecifiersNode = node.FindChildByName("rankSpecifiers") as SyntaxTreeNode_Rule;
                if (rankSpecifiersNode == null || rankSpecifiersNode.m_iChildIndex > 0)
                {
                    var expressionListNode = node.NodeAt(1);
                    if (expressionListNode != null && expressionListNode.RuleName == "expressionList")
                        resultType = resultType.MakeArrayType(1 + expressionListNode.NumValidNodes / 2);
                }
                if (rankSpecifiersNode != null && rankSpecifiersNode.NumValidNodes != 0)
                {
                    for (var i = 1; i < rankSpecifiersNode.NumValidNodes; i += 2)
                    {
                        rank = 1;
                        while (i < rankSpecifiersNode.NumValidNodes && rankSpecifiersNode.ChildAt(i).IsLit(","))
                        {
                            ++rank;
                            ++i;
                        }
                        resultType = resultType.MakeArrayType(rank);
                    }
                }

                var initializerNode = node.NodeAt(-1);
                if (initializerNode != null && initializerNode.RuleName == "arrayInitializer")
                    ResolveNode(initializerNode);

                return (resultType ?? unknownType).GetThisInstance();

            case "implicitArrayCreationExpression":
                resultType = null;

                var rankSpecifierNode = node.NodeAt(0);
                rank = rankSpecifierNode != null && rankSpecifierNode.NumValidNodes > 0 ? rankSpecifierNode.NumValidNodes - 1 : 1;

                initializerNode = node.NodeAt(1);
                var elements = initializerNode != null ? ResolveNode(initializerNode) : null;
                if (elements != null)
                    resultType = (elements.TypeOf() as TypeDefinitionBase ?? unknownType).MakeArrayType(rank);

                return (resultType ?? unknownType).GetThisInstance();

            case "arrayInitializer":
                if (node.NumValidNodes >= 2)
                    if (!node.ChildAt(1).IsLit("}"))
                        return ResolveNode(node.ChildAt(1), scope);
                return unknownType;

            case "variableInitializerList":
                TypeDefinitionBase commonType = null;
                for (var i = 0; i < node.NumValidNodes; i += 2)
                {
                    var type = (ResolveNode(node.ChildAt(i), scope) ?? unknownSymbol).TypeOf() as TypeDefinitionBase;
                    if (type != null)
                    {
                        if (commonType == null)
                        {
                            commonType = type;
                        }
                        else
                        {
                            // HACK!!!
                            if (commonType.DerivesFrom(type))
                                commonType = type;
                        }
                    }
                }
                return commonType;

            case "variableInitializer":
                return ResolveNode(node.ChildAt(0), scope);

            case "conditionalOrExpression":
                if (node.NumValidNodes == 1)
                {
                    node = node.NodeAt(0);
                    goto case "conditionalAndExpression";
                }
                for (var i = 0; i < node.NumValidNodes; i += 2)
                    ResolveNode(node.ChildAt(i), scope);
                return builtInTypes_bool;

            case "conditionalAndExpression":
                if (node.NumValidNodes == 1)
                {
                    node = node.NodeAt(0);
                    goto case "inclusiveOrExpression";
                }
                for (var i = 0; i < node.NumValidNodes; i += 2)
                    ResolveNode(node.ChildAt(i), scope);
                return builtInTypes_bool;

            case "equalityExpression":
                if (node.NumValidNodes == 1)
                {
                    node = node.NodeAt(0);
                    goto case "relationalExpression";
                }
                for (var i = 0; i < node.NumValidNodes; i += 2)
                    ResolveNode(node.ChildAt(i), scope);
                return builtInTypes_bool;

            case "relationalExpression":
                if (node.NumValidNodes == 1)
                {
                    node = node.NodeAt(0);
                    goto case "shiftExpression";
                }
                part = ResolveNode(node.ChildAt(0), scope);
                for (var i = 2; i < node.NumValidNodes; i += 2)
                {
                    if (node.ChildAt(i - 1).IsLit("as"))
                    {
                        part = ResolveNode(node.ChildAt(i), scope);
                        if (part is TypeDefinitionBase)
                            part = (part as TypeDefinitionBase).GetThisInstance();
                    }
                    else
                    {
                        ResolveNode(node.ChildAt(i), scope);
                        part = builtInTypes_bool.GetThisInstance();
                    }
                }
                return part;

            case "booleanExpression":
                ResolveNode(node.ChildAt(0), scope);
                return builtInTypes_bool;

            case "lambdaExpression":
                ResolveNode(node.ChildAt(0), scope);
                var nodeScope = node.scope as Scope_SymbolDeclaration;
                if (nodeScope != null && nodeScope.declaration != null)
                    return nodeScope.declaration.definition;
                return unknownSymbol;

            case "lambdaExpressionBody":
                var expressionNode = node.NodeAt(0);
                if (expressionNode != null)
                    return ResolveNode(expressionNode);
                return null;

            case "objectCreationExpression":
                var objectType = (ResolveNode(node.FindPreviousNode(), scope) ?? unknownType).TypeOf() as TypeDefinitionBase;
                return objectType != null ? objectType.GetThisInstance() : null;

            case "queryExpression":
                var queryBodyNode = node.NodeAt(1);
                if (queryBodyNode != null)
                {
                    var selectClauseNode = queryBodyNode.FindChildByName("selectClause") as SyntaxTreeNode_Rule;
                    if (selectClauseNode != null)
                    {
                        var selectExpressionNode = selectClauseNode.NodeAt(1);
                        if (selectExpressionNode != null)
                        {
                            var element = ResolveNode(selectExpressionNode);
                            if (element != null)
                            {
                                var elementType = element.TypeOf() as TypeDefinitionBase;
                                if (elementType != null)
                                {
                                    var genericType = builtInTypes_IEnumerable_1.ConstructType(new[] { new SymbolReference(elementType) });
                                    return genericType.GetThisInstance();
                                }
                            }
                        }
                    }
                }
                return unknownSymbol;

            case "qid":
                for (var i = 0; i < node.NumValidNodes; i++)
                {
                    asMemberOf = ResolveNode(node.ChildAt(i), scope, asMemberOf);
                    if (asMemberOf == null || asMemberOf.kind == SymbolKind.Error)
                        break;
                }
                return asMemberOf ?? unknownSymbol;

            case "qidStart":
                if (node.NumValidNodes == 1)
                    return ResolveNode(node.ChildAt(0), scope);
                if (node.NumValidNodes == 2 && node.NodeAt(1) != null)
                {
                    ResolveNode(node.ChildAt(1), scope);
                    return ResolveNode(node.ChildAt(0), scope, null, node.NodeAt(1).NumValidNodes / 3, true);
                }
                asMemberOf = ResolveNode(node.ChildAt(0), scope);
                if (asMemberOf != null && asMemberOf.kind != SymbolKind.Error && node.NumValidNodes == 3)
                    return ResolveNode(node.ChildAt(2), scope, asMemberOf);
                return unknownSymbol;

            case "qidPart":
                return ResolveNode(node.ChildAt(0), scope, asMemberOf);

            case "classMemberDeclaration":
                return null;

            case "implicitAnonymousFunctionParameterList":
            case "implicitAnonymousFunctionParameter":
            case "explicitAnonymousFunctionSignature":
            case "explicitAnonymousFunctionParameterList":
            case "explicitAnonymousFunctionParameter":
            case "anonymousFunctionSignature":
            case "typeParameterList":
            case "constructorInitializer":
            case "interfaceMemberDeclaration":
            case "collectionInitializer":
            case "elementInitializerList":
            case "elementInitializer":
            case "methodHeader":
                return null;

            default:
                return null;
        }

        return null;
    }

    protected virtual SymbolDefinition GetIndexer(TypeDefinitionBase[] argumentTypes)
    {
        return null;
    }

    public virtual SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        SymbolDefinition definition;
        if (!members.TryGetValue(memberName, numTypeParameters, out definition))
        {
            var marker = memberName.IndexOf('`');
            if (marker > 0)
            {
                Debug.LogError("FindName!!! " + memberName);
                members.TryGetValue(memberName.Substring(0, marker), numTypeParameters, out definition);
            }
        }
        if (asTypeOnly && definition != null && definition.kind != SymbolKind.Namespace && !(definition is TypeDefinitionBase))
            return null;
        return definition;
    }

    public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        var tp = GetTypeParameters();
        if (tp != null)
        {
            for (var i = 0; i < tp.Count; ++i)
            {
                SD_Type_Parameter p = tp[i];
                if (!data.ContainsKey(p.name))
                    data.Add(p.name, p);
            }
        }

        GetMembersCompletionData(data, fromInstance ? 0 : BindingFlags.Static, AccessLevelMask.Any, assembly);
        //	base.GetCompletionData(data, assembly);
    }

    public virtual void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        if ((mask & AccessLevelMask.Public) != 0)
        {
            if (assembly.InternalsVisibleIn(this.Assembly))
                mask |= AccessLevelMask.Internal;
            else
                mask &= ~AccessLevelMask.Internal;
        }

        flags = flags & (BindingFlags.Static | BindingFlags.Instance);
        bool onlyStatic = flags == BindingFlags.Static;
        bool onlyInstance = flags == BindingFlags.Instance;

        foreach (var m in members)
        {
            if (m.kind == SymbolKind.Namespace)
            {
                if (!data.ContainsKey(m.ReflectionName))
                    data.Add(m.ReflectionName, m);
            }
            else if (m.kind != SymbolKind.MethodGroup)
            {
                if ((onlyStatic ? !m.IsInstanceMember : onlyInstance ? m.IsInstanceMember : true)
                    && m.IsAccessible(mask)
                    && m.kind != SymbolKind.Constructor && m.kind != SymbolKind.Destructor && m.kind != SymbolKind.Indexer
                    && !data.ContainsKey(m.ReflectionName))
                {
                    data.Add(m.ReflectionName, m);
                }
            }
            else
            {
                var methodGroup = m as SD_MethodGroup;
                foreach (var method in methodGroup.methods)
                    if ((onlyStatic ? method.IsStatic : onlyInstance ? !method.IsStatic : true)
                        && method.IsAccessible(mask)
                        && method.kind != SymbolKind.Constructor && method.kind != SymbolKind.Destructor && method.kind != SymbolKind.Indexer
                        && !data.ContainsKey(m.ReflectionName))
                    {
                        data.Add(m.ReflectionName, method);
                    }
            }
        }
    }

    //public virtual bool IsGeneric
    //{
    //	get
    //	{
    //		return false;
    //	}
    //}

    public virtual bool IsSameType(TypeDefinitionBase type)
    {
        return type == this;
    }

    public bool IsSameOrParentOf(TypeDefinitionBase type)
    {
        var constructedType = this as SD_Type_Constructed;
        var thisType = constructedType != null ? constructedType.genericTypeDefinition : this;
        while (type != null)
        {
            if (type == thisType)
                return true;
            constructedType = type as SD_Type_Constructed;
            type = (constructedType != null ? constructedType.genericTypeDefinition : type).parentSymbol as TypeDefinitionBase;
        }
        return false;
    }

    public virtual TypeDefinitionBase TypeOfTypeParameter(SD_Type_Parameter tp)
    {
        if (parentSymbol != null)
            return parentSymbol.TypeOfTypeParameter(tp);
        return tp;
    }

    public virtual bool IsAccessible(AccessLevelMask accessLevelMask)
    {
        if (accessLevelMask == AccessLevelMask.None)
            return false;
        if (IsPublic)
            return true;
        if (IsProtected && (accessLevelMask & AccessLevelMask.Protected) != 0)
            return true;
        if (IsInternal && (accessLevelMask & AccessLevelMask.Internal) != 0)
            return true;

        return (accessLevelMask & AccessLevelMask.Private) != 0;
    }

}

