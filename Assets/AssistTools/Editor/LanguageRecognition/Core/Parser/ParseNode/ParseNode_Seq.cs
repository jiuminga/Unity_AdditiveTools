using System.Collections.Generic;
using System.Text;
public class ParseNode_Seq : ParseNode_Base
{
    private readonly List<ParseNode_Base> nodes = new List<ParseNode_Base>();

    public ParseNode_Seq(params ParseNode_Base[] nodes)
    {
        foreach (var t in nodes)
            Add(t);
    }

    public override sealed void Add(ParseNode_Base node)
    {
        var idNode = node as ParseNode_Id;
        if (idNode != null)
            node = idNode.Clone();

        var seqNode = node as ParseNode_Seq;
        if (seqNode != null)
            foreach (var n in seqNode.nodes)
                Add(n);
        else
        {
            node.parent = this;
            node.childIndex = nodes.Count;
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
            if (nodes.Count == 0)
                FirstSet.AddEmpty();
            else
                for (int i = 0; i < nodes.Count; ++i)
                {
                    var t = nodes[i];
                    var set = t.Init_PreCheckSet(parser);
                    FirstSet.Add(set);
                    if (!set.ContainsEmpty())
                    {
                        FirstSet.RemoveEmpty();
                        break;
                    }
                }
        }
        return FirstSet;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        Init_PreCheckSet(parser);
        FollowSet = succ;
        for (var n = nodes.Count; n-- > 0;)
        {
            var prev = nodes[n].Init_FollowSet(parser, succ);
            if (prev.ContainsEmpty())
            {
                prev = new TokenSet(prev);
                prev.RemoveEmpty();
                prev.Add(succ);
            }
            succ = prev;
        }
        return FirstSet;
    }

    public override void CheckLL1(ParseNode_Root parser)
    {
        base.CheckLL1(parser);
        foreach (var t in nodes)
            t.CheckLL1(parser);
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        foreach (var t in nodes)
        {
            if (!pSyntaxTreeBuilder.KeepScanning)
                return true;
            if (!t.Scan(pSyntaxTreeBuilder))
                return false;
        }
        return true;
    }

    public override ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        var index = child.childIndex;
        if (++index < nodes.Count)
            return nodes[index];
        return base.NextAfterChild(this, pSyntaxTreeBuilder);
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return nodes[0].Parse(pSyntaxTreeBuilder);
    }

    public override string ToString()
    {
        var s = new StringBuilder("( ");
        foreach (var t in nodes)
            s.Append(" " + t);
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
    }
}
