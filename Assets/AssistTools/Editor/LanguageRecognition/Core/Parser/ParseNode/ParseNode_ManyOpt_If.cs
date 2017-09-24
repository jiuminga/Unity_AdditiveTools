using System;
public class ParseNode_ManyOpt_If : ParseNode_Many_Opt
{
    protected readonly Predicate<SyntaxTreeBuilder> predicate;
    protected readonly ParseNode_Base nodePredicate;
    protected readonly bool debug;

    public ParseNode_ManyOpt_If(Predicate<SyntaxTreeBuilder> pred, ParseNode_Base node, bool debug = false)
        : base(node)
    {
        predicate = pred;
        this.debug = debug;
    }

    public ParseNode_ManyOpt_If(ParseNode_Base pred, ParseNode_Base node, bool debug = false)
        : base(node)
    {
        nodePredicate = pred;
        this.debug = debug;
    }

    public override ParseNode_Base GetNode()
    {
        //    if (node is Some)	// [ { n } ] -> [{ n }]
        //        return new Many(node.GetNode());

        //    if (node is Opt)	// [ [ n ] ] -> [ n ]
        //        return node;

        //    if (node is Many)	// [ [{ n }] ] -> [{ n }]
        //        return node;

        return this;
    }

    public virtual bool CheckPredicate(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        //if (debug)
        //{
        //    var s = pSyntaxTreeBuilder.Clone();
        //    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
        //    s.MoveNext();
        //    Debug.Log(s.Current.tokenKind + " " + s.Current.text);
        //}
        if (predicate != null)
            return predicate(pSyntaxTreeBuilder);
        else if (nodePredicate != null)
        {
            return pSyntaxTreeBuilder.PreCheck(nodePredicate);//, new GoalAdapter());
        }
        return false;
    }

    public override bool Matches(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current) && CheckPredicate(pSyntaxTreeBuilder);
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;

        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId) && CheckPredicate(pSyntaxTreeBuilder))
            return node.Scan(pSyntaxTreeBuilder);
        return true;
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId) && CheckPredicate(pSyntaxTreeBuilder))
            return node.Parse(pSyntaxTreeBuilder);
        else
            return parent.NextAfterChild(this, pSyntaxTreeBuilder); // .Parse2(pSyntaxTreeBuilder, goal);
    }

    // lookahead doesn't include empty.

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
            FirstSet = new TokenSet(node.Init_PreCheckSet(parser));
        return FirstSet;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        if (nodePredicate != null && nodePredicate.FollowSet == null)
            nodePredicate.Init_FollowSet(parser, new TokenSet());
        return base.Init_FollowSet(parser, succ);
    }

    public override string ToString()
    {
        return "[ ?(" + predicate + ") " + node + " ]";
    }
}
