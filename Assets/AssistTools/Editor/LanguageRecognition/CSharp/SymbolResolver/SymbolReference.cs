public class SymbolReference
{
    protected SymbolReference() { }

    public SymbolReference(SyntaxTreeNode_Base node)
    {
        parseTreeNode = node;
    }

    public SymbolReference(SymbolDefinition definedSymbol)
    {
        _definition = definedSymbol;
    }

    protected SyntaxTreeNode_Base parseTreeNode;
    public SyntaxTreeNode_Base Node { get { return parseTreeNode; } }

    protected uint _resolvedVersion;
    protected SymbolDefinition _definition;
    protected bool resolving = false;
    public static bool dontResolveNow = false;

    public virtual SymbolDefinition Definition
    {
        get
        {
            if (_definition != null &&
                (parseTreeNode != null && _resolvedVersion != LR_SyntaxTree.resolverVersion || !_definition.IsValid()))
                _definition = null;

            if (_definition == null)
            {
                if (!resolving)
                {
                    if (dontResolveNow)
                        return SymbolDefinition.unknownSymbol;
                    resolving = true;
                    _definition = SymbolDefinition.ResolveNode(parseTreeNode);
                    _resolvedVersion = LR_SyntaxTree.resolverVersion;
                    resolving = false;
                }
                else
                {
                    return SymbolDefinition.unknownSymbol;
                }
                if (_definition == null)
                {
                    _definition = SymbolDefinition.unknownType;
                    _resolvedVersion = LR_SyntaxTree.resolverVersion;
                }
            }
            return _definition;
        }
    }

    public bool IsBefore(SyntaxTreeNode_Leaf leaf)
    {
        if (parseTreeNode == null)
            return true;
        var lastLeaf = parseTreeNode as SyntaxTreeNode_Leaf;
        if (lastLeaf == null)
            lastLeaf = ((SyntaxTreeNode_Rule)parseTreeNode).GetLastLeaf();
        return lastLeaf != null && (lastLeaf.Line < leaf.Line || lastLeaf.Line == leaf.Line && lastLeaf.TokenIndex < leaf.TokenIndex);
    }

    public override string ToString()
    {
        return parseTreeNode != null ? parseTreeNode.Print() : _definition.GetName();
    }
}

