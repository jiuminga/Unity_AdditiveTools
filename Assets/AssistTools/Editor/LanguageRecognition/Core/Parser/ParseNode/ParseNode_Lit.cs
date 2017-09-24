using System.Collections.Generic;
public class ParseNode_Lit : ParseNode_Base
{
    public readonly string body;
    public string pretty;

    public ParseNode_Lit(string body)
    {
        pretty = body;
        this.body = body.Trim();
    }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        return FirstSet ?? (FirstSet = new TokenSet(parser.TokenToId(body)));
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        return Init_PreCheckSet(parser);
    }

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
        return "\"" + body + "\"";
    }

    public sealed override IEnumerable<ParseNode_Lit> EnumerateLitNodes()
    {
        yield return this;
    }
}
