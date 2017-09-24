using UnityEngine;
using System.Collections.Generic;
using System.Text;
public class ParseNode_Alt : ParseNode_Base
{
    protected List<ParseNode_Base> nodes = new List<ParseNode_Base>();

    public ParseNode_Alt(params ParseNode_Base[] nodes)
    {
        foreach (var node in nodes)
            Add(node);
    }

    public override sealed void Add(ParseNode_Base node)
    {
        var idNode = node as ParseNode_Id;
        if (idNode != null)
            node = idNode.Clone();

        var altNode = node as ParseNode_Alt;
        if (altNode != null)
        {
            foreach (var n in altNode.nodes)
            {
                n.parent = this;
                nodes.Add(n);
            }
        }
        else
        {
            node.parent = this;
            nodes.Add(node);
        }
    }

    public override ParseNode_Base GetNode()
    {
        return nodes.Count == 1 ? nodes[0] : this;
    }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
        {
            FirstSet = new TokenSet();
            foreach (var t in nodes)
            {
                if (t is ParseNode_ManyOpt_If)
                    continue;
                var set = t.Init_PreCheckSet(parser);
                if (FirstSet.Accepts(set))
                {
                    Debug.LogError(this + ": ambiguous alternatives");
                    Debug.LogWarning(FirstSet.Intersecton(set).ToString(parser));
                }
                FirstSet.Add(set);
            }
            foreach (var t in nodes)
            {
                if (t is ParseNode_ManyOpt_If)
                {
                    var set = t.Init_PreCheckSet(parser);
                    FirstSet.Add(set);
                }
            }
        }
        return FirstSet;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        Init_PreCheckSet(parser);
        FollowSet = succ;
        foreach (var node in nodes)
            node.Init_FollowSet(parser, succ);
        return FirstSet;
    }

    public override void CheckLL1(ParseNode_Root parser)
    {
        base.CheckLL1(parser);
        foreach (var node in nodes)
            node.CheckLL1(parser);
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;

        foreach (var node in nodes)
            if (node.Matches(pSyntaxTreeBuilder))
                return node.Scan(pSyntaxTreeBuilder);

        if (!FirstSet.ContainsEmpty())
            return false; 
        return true;
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        foreach (var node in nodes)
        {
            if (node.Matches(pSyntaxTreeBuilder))
            {
                return node.Parse(pSyntaxTreeBuilder);
            }
        }
        if (FirstSet.ContainsEmpty())
            return NextAfterChild(this, pSyntaxTreeBuilder);

        pSyntaxTreeBuilder.SyntaxErrorExpected(FirstSet);
        return this;
    }

    public override string ToString()
    {
        var s = new StringBuilder("( " + nodes[0]);
        for (var n = 1; n < nodes.Count; ++n)
            s.Append(" | " + nodes[n]);
        s.Append(" )");
        return s.ToString();
    }

    public sealed override IEnumerable<ParseNode_Lit> EnumerateLitNodes()
    {
        foreach (var node in nodes)
            foreach (var subnode in node.EnumerateLitNodes())
                yield return subnode;
    }

    public sealed override IEnumerable<ParseNode_Id> EnumerateIdNodes()
    {
        foreach (var node in nodes)
            foreach (var subnode in node.EnumerateIdNodes())
                yield return subnode;
    }

    public override IEnumerable<T> EnumerateNodesOfType<T>()
    {
        foreach (var node in nodes)
            foreach (var subnode in node.EnumerateNodesOfType<T>())
                yield return subnode;
        base.EnumerateNodesOfType<T>();
    }
}
