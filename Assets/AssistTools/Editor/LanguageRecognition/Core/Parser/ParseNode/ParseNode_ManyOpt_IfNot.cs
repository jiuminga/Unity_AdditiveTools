public class ParseNode_ManyOpt_IfNot : ParseNode_ManyOpt_If
{
    //public IfNot(Predicate<IpSyntaxTreeBuilder> pred, Node node)
    //    : base(pred, node)
    //{
    //}

    public ParseNode_ManyOpt_IfNot(ParseNode_Base pred, ParseNode_Base node)
        : base(pred, node)
    {
    }

    public override bool CheckPredicate(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return !base.CheckPredicate(pSyntaxTreeBuilder);
    }
}
