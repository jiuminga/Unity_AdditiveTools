using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class SyntaxTreeNode_Rule : SyntaxTreeNode_Base
{
    protected List<SyntaxTreeNode_Base> nodes = new List<SyntaxTreeNode_Base>();
    public Scope_Base scope;
    public SymbolDeclaration declaration;

    public IEnumerable<SyntaxTreeNode_Base> Nodes { get { return nodes; } }
    public int NumValidNodes { get; protected set; }

    public SemanticFlags Semantics
    {
        get
        {
            try
            {
                var peer = ((ParseNode_Id)ParseNode).LinkedTarget;
                if (peer == null)
                    Debug.Log("no peer for " + ParseNode);
                return peer != null ? ((ParseNode_Rule)peer).semantics : SemanticFlags.None;
            }
            catch (System.Exception)
            {

                throw;
            }

        }
    }

    public string RuleName
    {
        get { return ((ParseNode_Id)ParseNode).Name; }
    }

    public SyntaxTreeNode_Rule(ParseNode_Id rule)
    {
        ParseNode = rule;
    }

    public SyntaxTreeNode_Base ChildAt(int index)
    {
        if (index < 0)
            index += NumValidNodes;
        return index >= 0 && index < NumValidNodes ? nodes[index] : null;
    }

    public SyntaxTreeNode_Leaf LeafAt(int index)
    {
        if (index < 0)
            index += NumValidNodes;
        return index >= 0 && index < NumValidNodes ? nodes[index] as SyntaxTreeNode_Leaf : null;
    }

    public SyntaxTreeNode_Rule NodeAt(int index)
    {
        if (index < 0)
            index += NumValidNodes;
        return index >= 0 && index < NumValidNodes ? nodes[index] as SyntaxTreeNode_Rule : null;
    }

    public override bool Accept(IHierarchicalVisitor<SyntaxTreeNode_Rule, SyntaxTreeNode_Leaf> visitor)
    {
        if (visitor.VisitEnter(this))
        {
            foreach (var child in nodes)
                if (!child.Accept(visitor))
                    break;
        }
        return visitor.VisitLeave(this);
    }

    public SyntaxTreeNode_Leaf AddToken(TokenScanner pTokenScanner)
    {
        if (NumValidNodes < nodes.Count)
        {
            var reused = nodes[NumValidNodes] as SyntaxTreeNode_Leaf;
            if (reused != null && reused.TryReuse(pTokenScanner))
            {
                ++NumValidNodes;
                return reused;
            }
        }
        var leaf = new SyntaxTreeNode_Leaf(pTokenScanner) { Parent = this, m_iChildIndex = NumValidNodes };
        if (NumValidNodes == nodes.Count)
        {
            nodes.Add(leaf);
            ++NumValidNodes;
        }
        else
        {
            nodes.Insert(NumValidNodes++, leaf);
            for (var i = NumValidNodes; i < nodes.Count; ++i)
                ++nodes[i].m_iChildIndex;
        }

        return leaf;
    }

    public SyntaxTreeNode_Leaf AddToken(LexerToken token)
    {
        if (!token.IsMissing() && NumValidNodes < nodes.Count)
        {
            var reused = nodes[NumValidNodes] as SyntaxTreeNode_Leaf;
            if (reused != null && reused.token.text == token.text && reused.token.tokenKind == token.tokenKind)
            {
                reused.m_bMissing = false;
                reused.m_sSyntaxError = null;

                reused.token = token;
                reused.Parent = this;
                reused.m_iChildIndex = NumValidNodes;
                ++NumValidNodes;

                Debug.Log("reused " + reused.token + " from line " + (reused.Line + 1));
                return reused;
            }
        }

        var leaf = new SyntaxTreeNode_Leaf { token = token, Parent = this, m_iChildIndex = NumValidNodes };
        if (NumValidNodes == nodes.Count)
        {
            nodes.Add(leaf);
            ++NumValidNodes;
        }
        else
        {
            nodes.Insert(NumValidNodes++, leaf);
            for (var i = NumValidNodes; i < nodes.Count; ++i)
                ++nodes[i].m_iChildIndex;
        }
        return leaf;
    }

    public int InvalidateFrom(int index)
    {
        var numInvalidated = Mathf.Max(0, NumValidNodes - index);
        NumValidNodes -= numInvalidated;
        return numInvalidated;
    }

    public void RemoveNodeAt(int index)
    {
        if (index >= nodes.Count)
            return;

        nodes[index].Parent = null;

        if (index < NumValidNodes)
            --NumValidNodes;
        var node = nodes[index] as SyntaxTreeNode_Rule;
        if (node != null)
            node.Dispose();
        nodes.RemoveAt(index);
        for (var i = index; i < nodes.Count; ++i)
            --nodes[i].m_iChildIndex;
        if (Parent != null && !HasLeafs(false))
            Parent.RemoveNodeAt(m_iChildIndex);
    }

    public SyntaxTreeNode_Rule AddNode(ParseNode_Id rule, SyntaxTreeBuilder pSyntaxTreeBuilder, out bool skipParsing)
    {
        skipParsing = false;

        bool removedReusable = false;

        if (NumValidNodes < nodes.Count)
        {
            var reusable = nodes[NumValidNodes] as SyntaxTreeNode_Rule;
            if (reusable != null)
            {
                var firstLeaf = reusable.GetFirstLeaf(false);
                if (reusable.ParseNode != rule)
                {
                    if (firstLeaf == null || firstLeaf.token == null || firstLeaf.Line <= pSyntaxTreeBuilder.TokenScanner.CurrentLine)
                    {
                        reusable.Dispose();
                        removedReusable = true;
                    }
                }
                else
                {
                    if (firstLeaf != null && firstLeaf.token != null && firstLeaf.Line > pSyntaxTreeBuilder.TokenScanner.CurrentLine)
                    {
                        // Ignore this node for now
                    }
                    else if (firstLeaf == null || firstLeaf.token != null && firstLeaf.m_sSyntaxError != null)
                    {
                        reusable.Dispose();
                        removedReusable = true;
                    }
                    else if (firstLeaf.token == pSyntaxTreeBuilder.TokenScanner.Current)
                    {
                        var lastLeaf = reusable.GetLastLeaf();
                        if (lastLeaf != null && !reusable.HasErrors())
                        {
                            if (lastLeaf.token != null)
                            {
                                ((SyntaxTreeBuilder_CSharp)pSyntaxTreeBuilder).MoveAfterLeaf(lastLeaf);
                                skipParsing = true;
                                ++NumValidNodes;
                                return pSyntaxTreeBuilder.SyntaxRule_Cur;
                            }
                        }
                        else
                        {
                            reusable.Dispose();
                            removedReusable = true;
                        }
                    }
                    else if (reusable.NumValidNodes == 0)
                    {
                        ++NumValidNodes;
                        reusable.m_sSyntaxError = null;
                        reusable.m_bMissing = false;
                        return reusable;
                    }
                    else if (pSyntaxTreeBuilder.TokenScanner.Current != null && (firstLeaf.token == null || firstLeaf.Line <= pSyntaxTreeBuilder.TokenScanner.CurrentLine))
                    {
                        reusable.Dispose();
                        if (firstLeaf.token == null || firstLeaf.Line == pSyntaxTreeBuilder.TokenScanner.CurrentLine)
                        {
                            removedReusable = true;
                        }
                        else
                        {
                            nodes.RemoveAt(NumValidNodes);
                            for (var i = NumValidNodes; i < nodes.Count; ++i)
                                --nodes[i].m_iChildIndex;
                            return AddNode(rule, pSyntaxTreeBuilder, out skipParsing);
                        }
                    }
                }
            }
        }

        var node = new SyntaxTreeNode_Rule(rule) { Parent = this, m_iChildIndex = NumValidNodes };
        if (NumValidNodes == nodes.Count)
        {
            nodes.Add(node);
            ++NumValidNodes;
        }
        else
        {
            if (removedReusable)
                nodes[NumValidNodes] = node;
            else
                nodes.Insert(NumValidNodes, node);
            ++NumValidNodes;
        }
        if (NumValidNodes < nodes.Count && nodes[NumValidNodes].m_iChildIndex != NumValidNodes)
            for (var i = NumValidNodes; i < nodes.Count; ++i)
                nodes[i].m_iChildIndex = i;
        return node;
    }

    public SyntaxTreeNode_Base FindChildByName(params string[] name)
    {
        SyntaxTreeNode_Base result = this;
        foreach (var n in name)
        {
            var node = result as SyntaxTreeNode_Rule;
            if (node == null)
                return null;

            var children = node.nodes;
            result = null;
            for (var i = 0; i < node.NumValidNodes; i++)
            {
                var child = children[i];
                if (child.ParseNode != null && child.ParseNode.ToString() == n)
                {
                    result = child;
                    break;
                }
            }
            if (result == null)
                return null;
        }
        return result;
    }

    public override void Dump(StringBuilder sb, int indent)
    {
        sb.Append(' ', 2 * indent);
        sb.Append(m_iChildIndex);
        sb.Append(' ');
        var id = ParseNode as ParseNode_Id;
        if (id != null && id.Rule != null)
        {
            if (m_sSyntaxError != null)
                sb.Append("? ");
            sb.AppendLine(id.Rule.NtName);
            if (m_sSyntaxError != null)
                sb.Append(' ').AppendLine(m_sSyntaxError);
        }

        ++indent;
        for (var i = 0; i < NumValidNodes; ++i)
            nodes[i].Dump(sb, indent);
    }

    public override string Print()
    {
        var result = string.Empty;
        for (var i = 0; i < NumValidNodes; i++)
        {
            var child = nodes[i];
            result += child.Print();
        }
        return result;
    }

    public override bool IsLit(string litText)
    {
        return false;
    }

    public override bool HasLeafs(bool validNodesOnly = true)
    {
        var count = validNodesOnly ? NumValidNodes : nodes.Count;
        for (var i = 0; i < count; i++)
        {
            var n = nodes[i];
            if (n.HasLeafs(validNodesOnly))
                return true;
        }
        return false;
    }

    public override bool HasErrors(bool validNodesOnly = true)
    {
        var count = validNodesOnly ? NumValidNodes : nodes.Count;
        for (int i = 0; i < count; i++)
        {
            var n = nodes[i];
            if (n.HasErrors(validNodesOnly))
                return true;
        }
        return false;
    }

    public SyntaxTreeNode_Leaf GetFirstLeaf(bool validNodesOnly = true)
    {
        var count = validNodesOnly ? NumValidNodes : nodes.Count;
        for (int i = 0; i < count; i++)
        {
            var child = nodes[i];
            var leaf = child as SyntaxTreeNode_Leaf;
            if (leaf != null)
                return leaf;
            leaf = ((SyntaxTreeNode_Rule)child).GetFirstLeaf(validNodesOnly);
            if (leaf != null)
                return leaf;
        }
        return null;
    }

    public SyntaxTreeNode_Leaf GetLastLeaf()
    {
        for (int i = NumValidNodes; i-- > 0;)
        {
            var child = nodes[i];
            var leaf = child as SyntaxTreeNode_Leaf;
            if (leaf != null)
            {
                if (leaf.token == null)
                {
                    continue;
                }
                return leaf;
            }
            leaf = ((SyntaxTreeNode_Rule)child).GetLastLeaf();
            if (leaf != null)
                return leaf;
        }
        return null;
    }

    public void Exclude()
    {
        if (nodes.Count != 1)
            return;
        Parent.nodes[m_iChildIndex] = nodes[0];
        nodes[0].Parent = Parent;
        nodes[0].m_iChildIndex = m_iChildIndex;
    }

    public void CleanUp()
    {
        for (var i = nodes.Count; i-- > 0;)
        {
            var child = nodes[i] as SyntaxTreeNode_Rule;
            if (child != null)
                child.CleanUp();
        }
        if (NumValidNodes < nodes.Count)
        {
            for (var j = nodes.Count; j-- > NumValidNodes;)
            {
                var child = nodes[j] as SyntaxTreeNode_Rule;
                if (child != null)
                    child.Dispose();
            }
            nodes.RemoveRange(NumValidNodes, nodes.Count - NumValidNodes);
        }
    }

    public void Dispose()
    {
        for (var i = nodes.Count; i-- > 0;)
        {
            var child = nodes[i] as SyntaxTreeNode_Rule;
            if (child != null)
                child.Dispose();
        }

        if (declaration != null && declaration.scope != null)
        {
            declaration.scope.RemoveDeclaration(declaration);
            ++LR_SyntaxTree.resolverVersion;
            if (LR_SyntaxTree.resolverVersion == 0)
                ++LR_SyntaxTree.resolverVersion;
        }
    }
}


