public class ParseNode_Id_Name : ParseNode_Id
{
    public ParseNode_Id_Name()
        : base("NAME")
    { }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
        {
            base.Init_PreCheckSet(parser);
            FirstSet.Add(new TokenSet(parser.TokenToId("IDENTIFIER")));
        }
        return FirstSet;
    }
}
