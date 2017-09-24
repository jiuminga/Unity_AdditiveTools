using System.Text;

public abstract class SyntaxTreeNode_Base : IVisitableTreeNode<SyntaxTreeNode_Rule, SyntaxTreeNode_Leaf>
{
    public SyntaxTreeNode_Rule Parent;
    public int m_iChildIndex;
    public ParseNode_Base ParseNode;
    public bool m_bMissing;
    public string m_sSyntaxError;
    public string m_sSemanticError;

    private uint _resolvedVersion = 1;
    private SymbolDefinition _resolvedSymbol;
    public SymbolDefinition ResolvedSymbol
    {
        get
        {
            if (_resolvedSymbol != null && _resolvedVersion != 0 &&
                (_resolvedVersion != LR_SyntaxTree.resolverVersion || !_resolvedSymbol.IsValid())
            )
                _resolvedSymbol = null;
            return _resolvedSymbol;
        }
        set
        {
            if (_resolvedVersion == 0)
            {
                return;
            }
            _resolvedVersion = LR_SyntaxTree.resolverVersion;
            _resolvedSymbol = value;
        }
    }

    public void SetDeclaredSymbol(SymbolDefinition symbol)
    {
        _resolvedSymbol = symbol;
        _resolvedVersion = 0;
    }

    public int Depth
    {
        get
        {
            var d = 0;
            for (var p = Parent; p != null; p = p.Parent, ++d) ;
            return d;
        }
    }

    public SyntaxTreeNode_Leaf FindPreviousLeaf()
    {
        var result = this;
        while (result.m_iChildIndex == 0 && result.Parent != null)
            result = result.Parent;
        if (result.Parent == null)
            return null;
        result = result.Parent.ChildAt(result.m_iChildIndex - 1);
        SyntaxTreeNode_Rule node;
        while ((node = result as SyntaxTreeNode_Rule) != null)
        {
            if (node.NumValidNodes == 0)
                return node.FindPreviousLeaf();
            result = node.ChildAt(node.NumValidNodes - 1);
        }
        return result as SyntaxTreeNode_Leaf;
    }

    public SyntaxTreeNode_Leaf FindNextLeaf()
    {
        var result = this;
        while (result.Parent != null && result.m_iChildIndex == result.Parent.NumValidNodes - 1)
            result = result.Parent;
        if (result.Parent == null)
            return null;
        result = result.Parent.ChildAt(result.m_iChildIndex + 1);
        SyntaxTreeNode_Rule node;
        while ((node = result as SyntaxTreeNode_Rule) != null)
        {
            if (node.NumValidNodes == 0)
                return node.FindNextLeaf();
            result = node.ChildAt(0);
        }
        return result as SyntaxTreeNode_Leaf;
    }

    public SyntaxTreeNode_Base FindPreviousNode()
    {
        var result = this;
        while (result.m_iChildIndex == 0 && result.Parent != null)
            result = result.Parent;
        if (result.Parent == null)
            return null;
        result = result.Parent.ChildAt(result.m_iChildIndex - 1);
        return result;
    }

    public bool IsAncestorOf(SyntaxTreeNode_Base node)
    {
        while (node != null)
            if (node.Parent == this)
                return true;
            else
                node = node.Parent;
        return false;
    }

    public SyntaxTreeNode_Rule FindParentByName(string ruleName)
    {
        var result = Parent;
        while (result != null && result.RuleName != ruleName)
            result = result.Parent;
        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        Dump(sb, 1);
        return sb.ToString();
    }

    public abstract bool Accept(IHierarchicalVisitor<SyntaxTreeNode_Rule, SyntaxTreeNode_Leaf> visitor);

    public abstract void Dump(StringBuilder sb, int indent);

    public abstract string Print();

    public abstract bool HasLeafs(bool validNodesOnly = true);

    public abstract bool HasErrors(bool validNodesOnly = true);

    public abstract bool IsLit(string litText);
}


