public class ParseNode_Token : ParseNode_Base
{
    protected string name;

    public ParseNode_Token(string name, TokenSet lookahead)
    {
        this.name = name;
        this.FirstSet = lookahead;
    }

    public ParseNode_Token Clone()
    {
        var clone = new ParseNode_Token(name, FirstSet);
        return clone;
    }

    // returns a one-element set,
    // initialized by the parser.
    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        return FirstSet;
    }

    // follow doesn't need to be set.
    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        return FirstSet;
    }

    // follow is not set; nothing to check.
    public override void CheckLL1(ParseNode_Root parser)
    {
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;

        if (!FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
            return false;
        pSyntaxTreeBuilder.TokenScanner.MoveNext();
        return true;
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
        {
            pSyntaxTreeBuilder.SyntaxErrorExpected(FirstSet);
            return this;
        }

        pSyntaxTreeBuilder.SyntaxRule_Cur.AddToken(pSyntaxTreeBuilder.TokenScanner).ParseNode = this;
        pSyntaxTreeBuilder.TokenScanner.MoveNext();
        if (pSyntaxTreeBuilder.ErrorMessage == null)
        {
            pSyntaxTreeBuilder.SyntaxRule_Err = pSyntaxTreeBuilder.SyntaxRule_Cur;
            pSyntaxTreeBuilder.ParseNode_Err = pSyntaxTreeBuilder.ParseNode_Cur;
        }

        return parent.NextAfterChild(this, pSyntaxTreeBuilder);
    }

    public override string ToString()
    {
        return name;
    }
}
