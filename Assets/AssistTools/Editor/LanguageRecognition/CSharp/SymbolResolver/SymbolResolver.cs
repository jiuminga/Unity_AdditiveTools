using System;
using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

public static class SymbolResolver
{
    public static void GetCompletions(IdentifierCompletionsType completionTypes, SyntaxTreeNode_Base parseTreeNode, HashSet<SymbolDefinition> completionSymbols, string assetPath)
    {
#if false
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		GetCompletions_Profiled(completionTypes, parseTreeNode, completionSymbols, assetPath);
		stopwatch.Stop();
		Debug.Log("GetCompletions: " + stopwatch.ElapsedMilliseconds + "ms");
	}
	
	public static void GetCompletions_Profiled(IdentifierCompletionsType completionTypes, ParseTree.BaseNode parseTreeNode, HashSet<SymbolDefinition> completionSymbols, string assetPath)
	{
#endif
        try
        {
            var d = new Dictionary<string, SymbolDefinition>();
            var assemblyDefinition = SD_Assembly.FromAssetPath(assetPath);

            if ((completionTypes & IdentifierCompletionsType.MemberName) != 0)
            {
                SyntaxTreeNode_Base targetNode = null;

                var node = parseTreeNode.Parent;
                if (node.RuleName != "objectOrCollectionInitializer")
                {
                    if (node.RuleName != "objectInitializer")
                    {
                        if (node.RuleName == "memberInitializerList")
                            node = node.Parent; // objectInitializer
                    }
                    node = node.Parent; // objectOrCollectionInitializer
                }
                node = node.Parent;
                if (node.RuleName == "objectCreationExpression")
                {
                    targetNode = node.Parent;
                }
                else // node is memberInitializer
                {
                    targetNode = node.LeafAt(0);
                }

                var targetDef = targetNode != null ? SymbolDefinition.ResolveNode(targetNode) : null;
                if (targetDef != null)
                {
                    GetMemberCompletions(targetDef, parseTreeNode, assemblyDefinition, d, false);

                    var filteredData = new Dictionary<string, SymbolDefinition>();
                    foreach (var kv in d)
                    {
                        var symbol = kv.Value;
                        if (symbol.kind == SymbolKind.Field && (symbol.modifiers & Modifiers.ReadOnly) == 0 ||
                            symbol.kind == SymbolKind.Property && symbol.FindName("set", 0, false) != null)
                        {
                            filteredData[kv.Key] = symbol;
                        }
                    }
                    d = filteredData;
                }

                var targetType = targetDef != null ? targetDef.TypeOf() as TypeDefinitionBase : null;
                if (targetType == null || !targetType.DerivesFrom(SymbolDefinition.builtInTypes_IEnumerable))
                {
                    completionSymbols.Clear();
                    completionSymbols.UnionWith(d.Values);
                    return;
                }
            }

            if ((completionTypes & IdentifierCompletionsType.Member) != 0)
            {
                var target = parseTreeNode.FindPreviousNode();
                if (target != null)
                {
                    var targetAsNode = target as SyntaxTreeNode_Rule;
                    if (targetAsNode != null && targetAsNode.RuleName == "primaryExpressionPart")
                    {
                        var node0 = targetAsNode.NodeAt(0);
                        if (node0 != null && node0.RuleName == "arguments")
                        {
                            target = target.FindPreviousNode();
                            targetAsNode = target as SyntaxTreeNode_Rule;
                        }
                    }
                    //Debug.Log(targetAsNode ?? target.parent);
                    ResolveNode(targetAsNode ?? target.Parent);
                    var targetDef = GetResolvedSymbol(targetAsNode ?? target.Parent);

                    GetMemberCompletions(targetDef, parseTreeNode, assemblyDefinition, d, true);
                }
            }
            else if (parseTreeNode == null)
            {
#if SI3_WARNINGS
				Debug.LogWarning(completionTypes);
#endif
            }
            else
            {
                Scope_Base.completionNode = parseTreeNode;
                Scope_Base.completionAssetPath = assetPath;

                if (parseTreeNode.IsLit("=>"))
                {
                    parseTreeNode = parseTreeNode.Parent.NodeAt(parseTreeNode.m_iChildIndex + 1) ?? parseTreeNode;
                }
                if (parseTreeNode.IsLit("]") && parseTreeNode.Parent.RuleName == "attributes")
                {
                    parseTreeNode = parseTreeNode.Parent.Parent.NodeAt(parseTreeNode.Parent.m_iChildIndex + 1);
                }

                var enclosingScopeNode = parseTreeNode as SyntaxTreeNode_Rule ?? parseTreeNode.Parent;
                if (enclosingScopeNode != null && (enclosingScopeNode.scope is Scope_SymbolDeclaration) &&
                    (parseTreeNode.IsLit(";") || parseTreeNode.IsLit("}")) &&
                    enclosingScopeNode.GetLastLeaf() == parseTreeNode)
                {
                    enclosingScopeNode = enclosingScopeNode.Parent;
                }
                while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
                    enclosingScopeNode = enclosingScopeNode.Parent;
                if (enclosingScopeNode != null)
                {
                    var lastLeaf = parseTreeNode as SyntaxTreeNode_Leaf ??
                        ((SyntaxTreeNode_Rule)parseTreeNode).GetLastLeaf() ??
                        ((SyntaxTreeNode_Rule)parseTreeNode).FindPreviousLeaf();
                    Scope_Base.completionAtLine = lastLeaf != null ? lastLeaf.Line : 0;
                    Scope_Base.completionAtTokenIndex = lastLeaf != null ? lastLeaf.TokenIndex : 0;

                    enclosingScopeNode.scope.GetCompletionData(d, true, assemblyDefinition);
                }
            }

            completionSymbols.UnionWith(d.Values);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public static SymbolDefinition GetResolvedSymbol(SyntaxTreeNode_Base baseNode)
    {
#if false
		var stopwatch = new Stopwatch();
		stopwatch.Start();
		var result = GetResolvedSymbol_Internal(baseNode);
		stopwatch.Stop();
		Debug.Log("GetResolvedSymbol: " + stopwatch.ElapsedMilliseconds + "ms");
		return result;
	}
	
	public static SymbolDefinition GetResolvedSymbol_Internal(ParseTree.BaseNode baseNode)
	{
#endif
        var leaf = baseNode as SyntaxTreeNode_Leaf;
        if (leaf != null)
        {
            if (leaf.ResolvedSymbol == null && leaf.Parent != null)
                ResolveNode(leaf.Parent);
            return leaf.ResolvedSymbol;
        }

        var node = baseNode as SyntaxTreeNode_Rule;
        if (node == null || node.NumValidNodes == 0)
            return null;

        switch (node.RuleName)
        {
            case "primaryExpressionStart":
                if (node.NumValidNodes < 3)
                    return GetResolvedSymbol(node.ChildAt(0));
                leaf = node.LeafAt(2);
                return leaf != null ? leaf.ResolvedSymbol : null;
            case "primaryExpressionPart":
                return GetResolvedSymbol(node.NodeAt(0));
            case "arguments":
                return GetResolvedSymbol(node.FindPreviousNode() as SyntaxTreeNode_Rule);
            case "objectCreationExpression":
                var newType = GetResolvedSymbol(node.FindPreviousNode() as SyntaxTreeNode_Rule);
                if (newType == null || newType.kind == SymbolKind.Error)
                    newType = SymbolDefinition.builtInTypes_object;
                var typeOfNewType = (TypeDefinitionBase)newType.TypeOf();
                return typeOfNewType.GetThisInstance();
            case "arrayCreationExpression":
                var elementType = GetResolvedSymbol(node.FindPreviousNode() as SyntaxTreeNode_Rule);
                var arrayInstance = SymbolDefinition.ResolveNode(node, null, elementType);
                return arrayInstance ?? SymbolDefinition.builtInTypes_Array.GetThisInstance();
            case "nonArrayType":
                var typeNameType = GetResolvedSymbol(node.NodeAt(0)) as TypeDefinitionBase;
                if (typeNameType == null || typeNameType.kind == SymbolKind.Error)
                    typeNameType = SymbolDefinition.builtInTypes_object;
                return node.NumValidNodes == 1 ? typeNameType : typeNameType.MakeNullableType();
            case "typeName":
                return GetResolvedSymbol(node.NodeAt(0));
            case "namespaceOrTypeName":
                return GetResolvedSymbol(node.NodeAt(node.NumValidNodes & ~1));
            case "accessIdentifier":
                leaf = node.NumValidNodes < 2 ? null : node.LeafAt(1);
                if (leaf != null && leaf.ResolvedSymbol == null)
                    SymbolResolver.ResolveNode(node);
                return leaf != null ? leaf.ResolvedSymbol : null;
            case "predefinedType":
            case "typeOrGeneric":
                return node.LeafAt(0).ResolvedSymbol;
            case "typeofExpression":
                return ((TypeDefinitionBase)ReflectedTypeReference.ForType(typeof(Type)).Definition).GetThisInstance();
            case "sizeofExpression":
                return SymbolDefinition.builtInTypes_int.GetThisInstance();
            case "localVariableType":
            case "brackets":
            case "expression":
            case "unaryExpression":
            case "parenExpression":
            case "checkedExpression":
            case "uncheckedExpression":
            case "defaultValueExpression":
            case "relationalExpression":
            case "inclusiveOrExpression":
            case "exclusiveOrExpression":
            case "andExpression":
            case "equalityExpression":
            case "shiftExpression":
            case "primaryExpression":
            case "type":
                return SymbolDefinition.ResolveNode(node, null, null, 0);
            default:
#if SI3_WARNINGS
				Debug.LogWarning(node.RuleName);
#endif
                return SymbolDefinition.ResolveNode(node, null, null, 0);
        }
    }

    private static void GetMemberCompletions( SymbolDefinition targetDef, SyntaxTreeNode_Base parseTreeNode,
        SD_Assembly assemblyDefinition,Dictionary<string, SymbolDefinition> d, bool includeExtensionMethods)
    {
        if (targetDef != null)
        {
            var typeOf = targetDef.TypeOf();
            var flags = BindingFlags.Instance | BindingFlags.Static;
            switch (targetDef.kind)
            {
                case SymbolKind.None:
                case SymbolKind.Error:
                    break;
                case SymbolKind.Namespace:
                case SymbolKind.Interface:
                case SymbolKind.Struct:
                case SymbolKind.Class:
                case SymbolKind.TypeParameter:
                case SymbolKind.Delegate:
                    flags = BindingFlags.Static;
                    break;
                case SymbolKind.Enum:
                    flags = BindingFlags.Static;
                    break;
                case SymbolKind.Field:
                case SymbolKind.ConstantField:
                case SymbolKind.LocalConstant:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Indexer:
                case SymbolKind.Method:
                case SymbolKind.MethodGroup:
                case SymbolKind.Constructor:
                case SymbolKind.Destructor:
                case SymbolKind.Operator:
                case SymbolKind.Accessor:
                case SymbolKind.Parameter:
                case SymbolKind.CatchParameter:
                case SymbolKind.Variable:
                case SymbolKind.ForEachVariable:
                case SymbolKind.FromClauseVariable:
                case SymbolKind.EnumMember:
                    flags = BindingFlags.Instance;
                    break;
                case SymbolKind.BaseTypesList:
                case SymbolKind.TypeParameterConstraintList:
                    flags = BindingFlags.Static;
                    break;
                case SymbolKind.Instance:
                    flags = BindingFlags.Instance;
                    break;
                case SymbolKind.Null:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            //targetDef.kind = targetDef is TypeDefinitionBase && targetDef.kind != SymbolKind.Enum ? BindingFlags.Static : targetDef is InstanceDefinition ? BindingFlags.Instance : 0;

            TypeDefinitionBase contextType = null;
            for (var n = parseTreeNode as SyntaxTreeNode_Rule ?? parseTreeNode.Parent; n != null; n = n.Parent)
            {
                var s = n.scope as Scope_SymbolDeclaration;
                if (s != null)
                {
                    contextType = s.declaration.definition as TypeDefinitionBase;
                    if (contextType != null)
                        break;
                }
            }

            AccessLevelMask mask =
                typeOf == contextType || typeOf.IsSameOrParentOf(contextType) ? AccessLevelMask.Private | AccessLevelMask.Protected | AccessLevelMask.Internal | AccessLevelMask.Public :
                contextType != null && contextType.DerivesFrom(typeOf as TypeDefinitionBase) ? AccessLevelMask.Protected | AccessLevelMask.Internal | AccessLevelMask.Public :
                AccessLevelMask.Internal | AccessLevelMask.Public;

            if (typeOf.Assembly == null || !typeOf.Assembly.InternalsVisibleIn(assemblyDefinition))
                mask &= ~AccessLevelMask.Internal;

            typeOf.GetMembersCompletionData(d, flags, mask, assemblyDefinition);

            if (includeExtensionMethods && flags == BindingFlags.Instance &&
                (typeOf.kind == SymbolKind.Class || typeOf.kind == SymbolKind.Struct || typeOf.kind == SymbolKind.Interface || typeOf.kind == SymbolKind.Enum))
            {
                var enclosingScopeNode = parseTreeNode as SyntaxTreeNode_Rule ?? parseTreeNode.Parent;
                while (enclosingScopeNode != null && enclosingScopeNode.scope == null)
                    enclosingScopeNode = enclosingScopeNode.Parent;
                var enclosingScope = enclosingScopeNode != null ? enclosingScopeNode.scope : null;

                if (enclosingScope != null)
                    enclosingScope.GetExtensionMethodsCompletionData(typeOf as TypeDefinitionBase, d);
            }
        }
    }

    public static SyntaxTreeNode_Rule ResolveNode(SyntaxTreeNode_Rule node)
    {
        if (node == null)
            return null;

        while (node.Parent != null)
        {
            switch (node.RuleName)
            {
                //case "primaryExpression":
                case "primaryExpressionStart":
                case "primaryExpressionPart":
                case "objectCreationExpression":
                case "objectOrCollectionInitializer":
                case "typeOrGeneric":
                case "namespaceOrTypeName":
                case "typeName":
                case "nonArrayType":
                //case "attribute":
                case "accessIdentifier":
                case "brackets":
                case "argumentList":
                case "attributeArgumentList":
                case "argumentName":
                case "attributeMemberName":
                case "argument":
                case "attributeArgument":
                case "attributeArguments":
                case "arrayCreationExpression":
                case "implicitArrayCreationExpression":
                case "arrayInitializer":
                case "arrayInitializerList":
                case "qidStart":
                case "qidPart":
                case "memberInitializer":
                case "globalNamespace":
                    node = node.Parent;
                    continue;
            }
            break;
        }

        try
        {
            var result = SymbolDefinition.ResolveNode(node, null, null, 0);//numTypeArgs);
            if (result == null)
                ResolveChildren(node);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }

        return node;
    }

    static void ResolveChildren(SyntaxTreeNode_Rule node)
    {
        if (node == null)
            return;
        if (node.NumValidNodes != 0)
        {
            for (var i = 0; i < node.NumValidNodes; ++i)
            {
                var child = node.ChildAt(i);

                var leaf = child as SyntaxTreeNode_Leaf;
                if (leaf == null ||
                    leaf.token != null &&
                    leaf.token.tokenKind != LexerToken.Kind.Punctuator &&
                    (leaf.token.tokenKind != LexerToken.Kind.Keyword || SymbolDefinition.builtInTypes.ContainsKey(leaf.token.text)))
                {
                    if (leaf == null)
                    {
                        switch (((SyntaxTreeNode_Rule)child).RuleName)
                        {
                            case "modifiers":
                            case "methodBody":
                                continue;
                        }
                    }
                    var numTypeArgs = 0;
                    if (SymbolDefinition.ResolveNode(child, null, null, numTypeArgs) == null)
                    {
                        var childAsNode = child as SyntaxTreeNode_Rule;
                        if (childAsNode != null)
                            ResolveChildren(childAsNode);
                    }
                }
            }
        }
    }
}

