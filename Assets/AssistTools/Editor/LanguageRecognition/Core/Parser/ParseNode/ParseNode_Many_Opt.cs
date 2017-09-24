public class ParseNode_Many_Opt : ParseNode_Many
{
    public ParseNode_Many_Opt(ParseNode_Base node)
        : base(node)
    {
    }

    public override ParseNode_Base GetNode()
    {
        //	if (node is Some)	// [ { n } ] -> [{ n }]
        //		return new Many(node.GetNode());

        if (node is ParseNode_Many_Opt)    // [ [ n ] ] -> [ n ]
            return node;

        if (node is ParseNode_Many)   // [ [{ n }] ] -> [{ n }]
            return node;

        return this;
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;

        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
            return node.Scan(pSyntaxTreeBuilder);
        return true;
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
            return node.Parse(pSyntaxTreeBuilder);
        return parent.NextAfterChild(this, pSyntaxTreeBuilder);
    }

    public override ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return parent != null ? parent.NextAfterChild(this, pSyntaxTreeBuilder) : null;
    }

    public override string ToString()
    {
        return "[ " + node + " ]";
    }
}
