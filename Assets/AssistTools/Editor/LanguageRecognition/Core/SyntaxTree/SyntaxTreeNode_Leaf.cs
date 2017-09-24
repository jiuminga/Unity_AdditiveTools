using System;
using System.Text;
using UnityEngine;

public class SyntaxTreeNode_Leaf : SyntaxTreeNode_Base
{
    public int Line
    {
        get { return token != null && token.formatedLine != null ? token.Line : 0; }
    }

    public int TokenIndex
    {
        get { return token != null && token.formatedLine != null ? token.formatedLine.tokens.IndexOf(token) : 0; }
    }

    public LexerToken token;

    public SyntaxTreeNode_Leaf() { }

    public SyntaxTreeNode_Leaf(TokenScanner pTokenScanner)
    {
        token = pTokenScanner.Current;
        token.m_kLinkedLeaf = this;
    }

    public bool TryReuse(TokenScanner pTokenScanner)
    {
        if (token == null)
            return false;
        var current = pTokenScanner.Current;
        if (current.m_kLinkedLeaf == this)
        {
            token.m_kLinkedLeaf = this;
            return true;
        }
        return false;
    }

    public override bool Accept(IHierarchicalVisitor<SyntaxTreeNode_Rule, SyntaxTreeNode_Leaf> visitor)
    {
        return visitor.Visit(this);
    }

    public override void Dump(StringBuilder sb, int indent)
    {
        sb.Append(' ', 2 * indent);
        sb.Append(m_iChildIndex);
        sb.Append(" ");
        if (m_sSyntaxError != null)
            sb.Append("? ");
        sb.Append(token);
        sb.Append(' ');
        sb.Append((Line + 1));
        sb.Append(':');
        sb.Append(TokenIndex);
        if (m_sSyntaxError != null)
            sb.Append(' ').Append(m_sSyntaxError);
        sb.AppendLine();
    }

    public void ReparseToken()
    {
        if (token != null)
        {
            token.m_kLinkedLeaf = null;
            token = null;
        }
        if (Parent != null)
            Parent.RemoveNodeAt(m_iChildIndex/*, false*/);
    }

    public override string Print()
    {
        var lit = ParseNode as ParseNode_Lit;
        if (lit != null)
            return lit.pretty;
        return token != null ? token.text : "";
    }

    public override bool IsLit(string litText)
    {
        var lit = ParseNode as ParseNode_Lit;
        return lit != null && lit.body == litText;
    }

    public override bool HasLeafs(bool validNodesOnly = true)
    {
        return true;
    }

    public override bool HasErrors(bool validNodesOnly = true)
    {
        return m_sSyntaxError != null;
    }
}


