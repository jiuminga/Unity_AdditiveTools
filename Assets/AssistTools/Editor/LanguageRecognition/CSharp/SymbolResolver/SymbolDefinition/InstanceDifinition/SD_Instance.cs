using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Debug = UnityEngine.Debug;

public class SD_Instance : SymbolDefinition
{
    public SymbolReference type;
    private bool _resolvingTypeOf = false;

    public override SymbolDefinition TypeOf()
    {
        if (_resolvingTypeOf)
            return unknownType;
        _resolvingTypeOf = true;

        if (type != null && (type.Definition == null || !type.Definition.IsValid()))
            type = null;

        if (type != null && type.Definition.kind == SymbolKind.Error)
            type = null;

        if (type == null)
        {
            {
                SymbolDeclaration decl = declarations != null ? declarations.FirstOrDefault() : null;
                if (decl != null)
                {
                    SyntaxTreeNode_Base typeNode = null;
                    switch (decl.kind)
                    {
                        case SymbolKind.Parameter:
                            if (decl.parseTreeNode.RuleName == "implicitAnonymousFunctionParameter")
                            {
                                type = TypeOfImplicitParameter(decl);
                            }
                            else
                            {
                                typeNode = decl.parseTreeNode.FindChildByName("type");
                                type = typeNode != null ? new SymbolReference(typeNode) : null;//"System.Object" };
                            }
                            break;

                        case SymbolKind.Field:
                            typeNode = decl.parseTreeNode.Parent.Parent.Parent.FindChildByName("type");
                            type = typeNode != null ? new SymbolReference(typeNode) : null;//"System.Object" };
                            break;

                        case SymbolKind.EnumMember:
                            type = new SymbolReference(parentSymbol);
                            break;

                        case SymbolKind.ConstantField:
                        case SymbolKind.LocalConstant:
                            //typeNode = decl.parseTreeNode.parent.parent.ChildAt(1);
                            //break;
                            switch (decl.parseTreeNode.Parent.Parent.RuleName)
                            {
                                case "constantDeclaration":
                                case "localDeclaration_Constant":
                                    typeNode = decl.parseTreeNode.Parent.Parent.ChildAt(1);
                                    break;

                                default:
                                    typeNode = decl.parseTreeNode.Parent.Parent.Parent.FindChildByName("IDENTIFIER");
                                    break;
                            }
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        case SymbolKind.Property:
                        case SymbolKind.Indexer:
                            typeNode = decl.parseTreeNode.Parent.FindChildByName("type");
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        case SymbolKind.Event:
                            typeNode = decl.parseTreeNode.FindParentByName("eventDeclaration").ChildAt(1);
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        case SymbolKind.Variable:
                            if (decl.parseTreeNode != null && decl.parseTreeNode.Parent != null && decl.parseTreeNode.Parent.Parent != null)
                                typeNode = decl.parseTreeNode.Parent.Parent.FindChildByName("localVariableType");
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        case SymbolKind.ForEachVariable:
                            if (decl.parseTreeNode != null)
                                typeNode = decl.parseTreeNode.FindChildByName("localVariableType");
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        case SymbolKind.FromClauseVariable:
                            type = null;
                            if (decl.parseTreeNode != null)
                            {
                                typeNode = decl.parseTreeNode.FindChildByName("type");
                                type = typeNode != null
                                    ? new SymbolReference(typeNode)
                                    : new SymbolReference(EnumerableElementType(decl.parseTreeNode.NodeAt(-1)));
                            }
                            break;

                        case SymbolKind.CatchParameter:
                            if (decl.parseTreeNode != null)
                                typeNode = decl.parseTreeNode.Parent.FindChildByName("exceptionClassType");
                            type = typeNode != null ? new SymbolReference(typeNode) : null;
                            break;

                        default:
                            Debug.LogError(decl.kind);
                            break;
                    }
                }
            }
        }

        var result = type != null ? type.Definition : unknownType;
        _resolvingTypeOf = false;
        return result;
    }

    private SymbolReference TypeOfImplicitParameter(SymbolDeclaration declaration)
    {
        int index = 0;
        var node = declaration.parseTreeNode;
        if (node.Parent.RuleName == "implicitAnonymousFunctionParameterList")
        {
            index = node.m_iChildIndex / 2;
            node = node.Parent;
        }
        node = node.Parent; // anonymousFunctionSignature
        node = node.Parent; // lambdaExpression
        node = node.Parent; // nonAssignmentExpression
        node = node.Parent; // elementInitializer or expression
        if (node.RuleName == "elementInitializer")
        {
            node = node.Parent // elementInitializerList
                .Parent // collectionInitializer
                .Parent // objectOrCollectionInitializer
                .Parent // objectCreationExpression
                .Parent; // primaryExpression
            if (node.RuleName != "primaryExpression")
                return null;

            node = node.NodeAt(1);
            if (node == null || node.RuleName != "nonArrayType")
                return null;

            var collectionType = ResolveNode(node.ChildAt(0)).TypeOf() as TypeDefinitionBase;
            if (collectionType != null && collectionType.kind != SymbolKind.Error)
            {
                var enumerableType = collectionType.ConvertTo(builtInTypes_IEnumerable_1) as SD_Type_Constructed;

                var targetTypeReference = enumerableType == null || enumerableType.typeArguments == null ? null : enumerableType.typeArguments.FirstOrDefault();
                var targetType = targetTypeReference == null ? null : targetTypeReference.Definition;
                if (targetType != null && targetType.kind == SymbolKind.Delegate)
                {
                    var delegateParameters = targetType.GetParameters();
                    if (delegateParameters != null && index < delegateParameters.Count)
                    {
                        var type = delegateParameters[index].TypeOf();
                        type = type.SubstituteTypeParameters(targetType);
                        return new SymbolReference(type);
                    }
                }
            }
        }
        if (node.RuleName == "expression" && (node.Parent.RuleName == "localVariableInitializer" || node.Parent.RuleName == "variableInitializer"))
        {
            node = node.Parent.Parent;
            if (node.RuleName == "variableInitializerList")
            {
                node = node.Parent.Parent.Parent.NodeAt(1);
                if (node == null || node.RuleName != "nonArrayType")
                    return null;
            }
            else if (node.RuleName != "localVariableDeclarator" && node.RuleName != "variableDeclarator")
            {
                return null;
            }

            var targetSymbol = node.ChildAt(0).ResolvedSymbol ?? ResolveNode(node.ChildAt(0));
            if (targetSymbol != null && targetSymbol.kind != SymbolKind.Error)
            {
                var targetType = targetSymbol.kind == SymbolKind.Delegate ? targetSymbol : targetSymbol.TypeOf();
                if (targetType != null && targetType.kind == SymbolKind.Delegate)
                {
                    var delegateParameters = targetType.GetParameters();
                    if (delegateParameters != null && index < delegateParameters.Count)
                    {
                        var type = delegateParameters[index].TypeOf();
                        type = type.SubstituteTypeParameters(targetType);
                        return new SymbolReference(type);
                    }
                }
            }
        }
        else if (node.RuleName == "expression" && node.Parent.RuleName == "argumentValue")
        {
            node = node.Parent; // argumentValue
            if (node.m_iChildIndex == 0)
            {
                node = node.Parent; // argument
                var argumentIndex = node.m_iChildIndex / 2;

                node = node.Parent; // argumentList
                node = node.Parent; // arguments
                node = node.Parent; // constructorInitializer or attribute or primaryExpressionPart or objectCreationExpression
                if (node.RuleName == "primaryExpressionPart")
                {
                    SyntaxTreeNode_Leaf methodId = null;
                    node = node.Parent.NodeAt(node.m_iChildIndex - 1); // primaryExpressionStart or primaryExpressionPart
                    if (node.RuleName == "primaryExpressionStart")
                    {
                        methodId = node.LeafAt(0);
                    }
                    else // node.RuleName == "primaryExpressionPart"
                    {
                        node = node.NodeAt(0);
                        if (node.RuleName == "accessIdentifier")
                        {
                            methodId = node.LeafAt(1);
                        }
                    }
                    if (methodId != null && methodId.token.tokenKind == LexerToken.Kind.Identifier)
                    {
                        if (methodId.ResolvedSymbol == null || methodId.ResolvedSymbol.kind == SymbolKind.MethodGroup)
                            SymbolResolver.ResolveNode(node);

                        var method = methodId.ResolvedSymbol as MethodDefinition;
                        var constructedSymbol = methodId.ResolvedSymbol as SD_ConstructedReference;
                        if (method != null)
                        {
                            if (method.IsExtensionMethod)
                            {
                                var nodeLeft = methodId.Parent;
                                if (nodeLeft != null && nodeLeft.RuleName == "accessIdentifier")
                                {
                                    nodeLeft = nodeLeft.FindPreviousNode() as SyntaxTreeNode_Rule;
                                    if (nodeLeft != null)
                                    {
                                        if (nodeLeft.RuleName == "primaryExpressionPart" || nodeLeft.RuleName == "primaryExpressionStart")
                                        {
                                            var symbolLeft = SymbolResolver.GetResolvedSymbol(nodeLeft);
                                            if (symbolLeft != null && symbolLeft.kind != SymbolKind.Error && !(symbolLeft is TypeDefinitionBase))
                                                ++argumentIndex;
                                        }
                                        else
                                        {
                                            ++argumentIndex;
                                        }
                                    }
                                }
                            }

                            if (argumentIndex < method.parameters.Count)
                            {
                                var parameter = method.parameters[argumentIndex];
                                var parameterType = parameter.TypeOf();
                                if (parameterType.kind == SymbolKind.Delegate)
                                {
                                    parameterType = parameterType.SubstituteTypeParameters(method);
                                    var delegateParameters = parameterType.GetParameters();
                                    if (delegateParameters != null && index < delegateParameters.Count)
                                    {
                                        var type = delegateParameters[index].TypeOf();
                                        type = type.SubstituteTypeParameters(parameterType);
                                        //type = type.SubstituteTypeParameters(method);
                                        return new SymbolReference(type);
                                    }
                                }
                            }
                        }
                        else if (constructedSymbol != null && constructedSymbol.kind == SymbolKind.Method)
                        {
                            var genericMethod = constructedSymbol.referencedSymbol;
                            var parameters = genericMethod.GetParameters();
                            if (parameters != null && argumentIndex < parameters.Count)
                            {
                                var parameter = parameters[argumentIndex];
                                var parameterType = parameter.TypeOf();
                                if (parameterType.kind == SymbolKind.Delegate)
                                {
                                    parameterType = parameterType.SubstituteTypeParameters(constructedSymbol);
                                    var delegateParameters = parameterType.GetParameters();
                                    if (delegateParameters != null && index < delegateParameters.Count)
                                    {
                                        var type = delegateParameters[index].TypeOf();
                                        type = type.SubstituteTypeParameters(parameterType);
                                        //type = type.SubstituteTypeParameters(constructedSymbol);
                                        return new SymbolReference(type);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (asTypeOnly)
        {
            leaf.ResolvedSymbol = null;
            return;
        }

        TypeOf();
        if (type == null || type.Definition == null || type.Definition == unknownType || type.Definition == unknownSymbol)
        {
            leaf.ResolvedSymbol = null;
            return;
        }
        type.Definition.ResolveMember(leaf, context, numTypeArgs, false);
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        var instanceType = TypeOf();
        if (instanceType != null)
            instanceType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
    }
}

