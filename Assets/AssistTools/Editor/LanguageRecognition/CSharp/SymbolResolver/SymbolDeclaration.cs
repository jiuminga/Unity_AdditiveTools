using System.Text;

using Debug = UnityEngine.Debug;
public class SymbolDeclaration
{
    public SymbolDefinition definition;
    public Scope_Base scope;

    public SymbolKind kind;

    public SyntaxTreeNode_Rule parseTreeNode;
    public Modifiers modifiers;
    public int numTypeParameters;

    protected string name;

    public SymbolDeclaration() { }

    public SymbolDeclaration(string name)
    {
        this.name = name;
    }

    public bool IsValid()
    {
        var node = parseTreeNode;
        if (node != null)
        {
            while (node.Parent != null)
                node = node.Parent;
            if (node.RuleName == "compilationUnit")
                return true;
        }

        if (scope != null)
        {
            scope.RemoveDeclaration(this);
            ++LR_SyntaxTree.resolverVersion;
            if (LR_SyntaxTree.resolverVersion == 0)
                ++LR_SyntaxTree.resolverVersion;
        }
        else if (definition != null)
        {
            Debug.Log("Scope is null for declaration " + name + ". Removing " + definition);
            if (definition.parentSymbol != null)
                definition.parentSymbol.RemoveDeclaration(this);
        }
        scope = null;
        return false;
    }

    public bool IsPartial
    {
        get { return (modifiers & Modifiers.Partial) != 0; }
    }

    public SyntaxTreeNode_Base NameNode()
    {
        if (parseTreeNode == null || parseTreeNode.NumValidNodes == 0)
            return null;

        SyntaxTreeNode_Base nameNode = null;
        switch (parseTreeNode.RuleName)
        {
            case "Declaration_Namespace":
                nameNode = parseTreeNode.ChildAt(1);
                var nameNodeAsNode = nameNode as SyntaxTreeNode_Rule;
                if (nameNodeAsNode != null && nameNodeAsNode.NumValidNodes != 0)
                    nameNode = nameNodeAsNode.ChildAt(-1) ?? nameNode;
                break;

            case "Directive_UsingAlias":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "interfaceDeclaration":
            case "structDeclaration":
            case "classDeclaration":
            case "enumDeclaration":
                nameNode = parseTreeNode.ChildAt(1);
                break;

            case "delegateDeclaration":
                nameNode = parseTreeNode.ChildAt(2);
                break;

            case "eventDeclarator":
            case "eventWithAccessorsDeclaration":
            case "propertyDeclaration":
            case "interfacePropertyDeclaration":
            case "variableDeclarator":
            case "localVariableDeclarator":
            case "constantDeclarator":
            case "interfaceMethodDeclaration":
            case "catchExceptionIdentifier":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "methodDeclaration":
            case "constructorDeclaration":
                var methodHeaderNode = parseTreeNode.NodeAt(0);
                if (methodHeaderNode != null && methodHeaderNode.NumValidNodes > 0)
                    nameNode = methodHeaderNode.ChildAt(0);
                break;

            case "methodHeader":
            case "constructorDeclarator":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "destructorDeclarator":
                nameNode = parseTreeNode.FindChildByName("IDENTIFIER");
                break;

            case "fixedParameter":
            case "operatorParameter":
            case "parameterArray":
            case "explicitAnonymousFunctionParameter":
                nameNode = parseTreeNode.FindChildByName("NAME");
                break;

            case "implicitAnonymousFunctionParameter":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "typeParameter":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "enumMemberDeclaration":
                if (parseTreeNode.ChildAt(0) is SyntaxTreeNode_Rule)
                    nameNode = parseTreeNode.ChildAt(1);
                else
                    nameNode = parseTreeNode.ChildAt(0);
                break;

            case "statementList":
                return null;

            case "lambdaExpression":
            case "anonymousMethodExpression":
                return parseTreeNode;

            case "interfaceTypeList":
                nameNode = parseTreeNode.ChildAt(0);
                break;

            case "foreachStatement":
            case "fromClause":
                nameNode = parseTreeNode.FindChildByName("NAME");
                break;

            case "getAccessorDeclaration":
            case "interfaceGetAccessorDeclaration":
            case "setAccessorDeclaration":
            case "interfaceSetAccessorDeclaration":
            case "addAccessorDeclaration":
            case "removeAccessorDeclaration":
                nameNode = parseTreeNode.FindChildByName("IDENTIFIER");
                break;

            case "indexerDeclaration":
            case "interfaceIndexerDeclaration":
            case "labeledStatement":
                return parseTreeNode.ChildAt(0);

            case "conversionOperatorDeclarator":
            case "operatorDeclarator":
            case "Directive_UsingNamespace":
            case "typeParameterConstraintsClause":
                return null;

            default:
                Debug.LogWarning("Don't know how to extract symbol name from: " + parseTreeNode);
                return null;
        }
        return nameNode;
    }

    public string Name
    {
        get
        {
            if (name != null)
                return name;

            if (definition != null)
                return name = definition.name;

            if (kind == SymbolKind.Constructor)
                return name = ".ctor";
            if (kind == SymbolKind.Indexer)
                return name = "Item";
            if (kind == SymbolKind.LambdaExpression)
            {
                var cuNode = parseTreeNode;
                while (cuNode != null && !(cuNode.scope is Scope_CompilationUnit))
                    cuNode = cuNode.Parent;
                name = cuNode != null ? cuNode.scope.CreateAnonymousName() : scope.CreateAnonymousName();
                return name;
            }
            if (kind == SymbolKind.Accessor)
            {
                switch (parseTreeNode.RuleName)
                {
                    case "getAccessorDeclaration":
                    case "interfaceGetAccessorDeclaration":
                        return "get";
                    case "setAccessorDeclaration":
                    case "interfaceSetAccessorDeclaration":
                        return "set";
                    case "addAccessorDeclaration":
                        return "add";
                    case "removeAccessorDeclaration":
                        return "remove";
                }
            }

            var nameNode = NameNode();
            var asNode = nameNode as SyntaxTreeNode_Rule;
            if (asNode != null && asNode.NumValidNodes != 0 && asNode.RuleName == "memberName")
            {
                asNode = asNode.NodeAt(0);
                if (asNode != null && asNode.NumValidNodes != 0 && asNode.RuleName == "qid")
                {
                    asNode = asNode.NodeAt(-1);
                    if (asNode != null && asNode.NumValidNodes != 0)
                    {
                        if (asNode.RuleName == "qidStart")
                        {
                            nameNode = asNode.ChildAt(0);
                        }
                        else
                        {
                            asNode = asNode.NodeAt(0);
                            if (asNode != null && asNode.NumValidNodes != 0)
                            {
                                nameNode = asNode.ChildAt(1);
                            }
                        }
                    }
                }
            }
            var asLeaf = nameNode as SyntaxTreeNode_Leaf;
            if (asLeaf != null && asLeaf.token != null && asLeaf.token.tokenKind != LexerToken.Kind.Identifier)
                nameNode = null;
            name = nameNode != null ? nameNode.Print() : "UNKNOWN";
            return name;
        }
    }

    public string ReflectionName
    {
        get
        {
            if (numTypeParameters == 0)
                return Name;
            return Name + '`' + numTypeParameters;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        Dump(sb, string.Empty);
        return sb.ToString();
    }

    protected virtual void Dump(StringBuilder sb, string indent)
    {
        sb.AppendLine(indent + kind + " " + ReflectionName + " (" + GetType() + ")");
    }

    public bool HasAllModifiers(Modifiers mods)
    {
        return (modifiers & mods) == mods;
    }

    public bool HasAnyModifierOf(Modifiers mods)
    {
        return (modifiers & mods) != 0;
    }
}

