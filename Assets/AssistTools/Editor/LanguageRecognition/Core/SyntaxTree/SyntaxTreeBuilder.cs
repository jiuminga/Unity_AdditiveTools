using Debug = UnityEngine.Debug;

public abstract class SyntaxTreeBuilder
{
    public TokenScanner TokenScanner { get { return m_kTokenScanner; } }
    protected TokenScanner m_kTokenScanner;

    private static string cachedErrorMessage;
    private static ParseNode_Base cachedErrorParseNode;

    public abstract Parser_Base Parser { get; }

    public abstract SyntaxTreeBuilder Clone();

    public abstract bool PreCheck(ParseNode_Base node, int maxDistance = int.MaxValue);
    public abstract LexerToken Lookahead(int offset, bool skipWhitespace = true);

    public Scope_CompilationUnit ComplilationUnitScop = null;

    //public abstract LexerToken CurrentToken();

    public ParseNode_Base ParseNode_Cur { get; set; }
    public SyntaxTreeNode_Rule SyntaxRule_Cur { get; set; }

    public ParseNode_Base ParseNode_Err { get; set; }
    public SyntaxTreeNode_Rule SyntaxRule_Err { get; set; }

    public SyntaxTreeNode_Leaf ErrorToken { get; set; }
    public string ErrorMessage { get; set; }
    public abstract bool KeepScanning { get; }
    public bool Seeking { get; set; }

    public abstract void InsertMissingToken(string errorMessage);
    public abstract bool CollectCompletions(TokenSet tokenSet);
    public abstract void OnSemanticNodeClose(SyntaxTreeNode_Rule node);
    public abstract void SyntaxErrorExpected(TokenSet lookahead);

    public LR_SyntaxTree Build()
    {
        if (!TokenScanner.MoveNext())
            return null;

        var kSyntexTree = new LR_SyntaxTree();
        SyntaxRule_Cur = kSyntexTree.root = new SyntaxTreeNode_Rule(Parser.ParseRoot.RootID);
        ParseNode_Cur = Parser.ParseRoot.Rule_Start.Parse(this);

        SyntaxRule_Err = SyntaxRule_Cur;
        ParseNode_Err = ParseNode_Cur;

        while (ParseNode_Cur != null)
            if (!ParseStep())
                break;

        return kSyntexTree;
    }

    public bool ParseStep()
    {
        if (ParseNode_Cur == null)
            return false;

        var token = TokenScanner.Current;
        if (ErrorMessage == null)
        {
            while (ParseNode_Cur != null)
            {
                ParseNode_Cur = ParseNode_Cur.Parse(this);
                if (ErrorMessage != null || token != TokenScanner.Current)
                    break;
            }

            if (ErrorMessage == null && token != TokenScanner.Current)
            {
                SyntaxRule_Err = SyntaxRule_Cur;
                ParseNode_Err = ParseNode_Cur;
            }
        }
        if (ErrorMessage != null)
        {
            if (token.tokenKind == LexerToken.Kind.EOF)
            {
                return false;
            }

            var missingParseTreeNode = SyntaxRule_Cur;
            var missingParseNode = ParseNode_Cur;

            SyntaxRule_Cur = SyntaxRule_Err;
            ParseNode_Cur = ParseNode_Err;
            if (SyntaxRule_Cur != null)
            {
                var cpt = SyntaxRule_Cur;
                for (var i = cpt.NumValidNodes; i > 0 && !cpt.ChildAt(--i).HasLeafs();)
                    cpt.InvalidateFrom(i);
            }

            if (ParseNode_Cur != null)
            {
                int numSkipped;
                ParseNode_Cur = ParseNode_Cur.Recover(this, out numSkipped);
            }
            if (ParseNode_Cur == null)
            {
                if (token.m_kLinkedLeaf != null)
                    token.m_kLinkedLeaf.ReparseToken();
                new SyntaxTreeNode_Leaf(TokenScanner);

                if (cachedErrorParseNode == ParseNode_Err)
                {
                    token.m_kLinkedLeaf.m_sSyntaxError = cachedErrorMessage;
                }
                else
                {
                    token.m_kLinkedLeaf.m_sSyntaxError = "Unexpected token! Expected " + ParseNode_Err.FirstSet.ToString(Parser.ParseRoot);
                    cachedErrorMessage = token.m_kLinkedLeaf.m_sSyntaxError;
                    cachedErrorParseNode = ParseNode_Err;
                }

                ParseNode_Cur = ParseNode_Err;
                SyntaxRule_Cur = SyntaxRule_Err;

                if (!TokenScanner.MoveNext())
                {
                    return false;
                }
                ErrorMessage = null;
            }
            else
            {
                if (missingParseNode != null && missingParseTreeNode != null)
                {
                    SyntaxRule_Cur = missingParseTreeNode;
                    ParseNode_Cur = missingParseNode;
                }

                InsertMissingToken(ErrorMessage ?? ("Expected " + missingParseNode.FirstSet.ToString(Parser.ParseRoot)));

                if (missingParseNode != null && missingParseTreeNode != null)
                {
                    ErrorMessage = null;
                    ErrorToken = null;
                    SyntaxRule_Cur = missingParseTreeNode;
                    ParseNode_Cur = missingParseNode;
                    ParseNode_Cur = missingParseNode.parent.NextAfterChild(missingParseNode, this);
                }
                ErrorMessage = null;
                ErrorToken = null;
            }
        }

        return true;
    }

    public bool MoveAfterLeaf(SyntaxTreeNode_Leaf leaf)
    {
        if (leaf == null || leaf.ParseNode == null)
            return false;

        if (leaf.m_sSyntaxError != null)
        {
            Debug.LogError("Can't move after error node! " + leaf.m_sSyntaxError);
            return false;
        }

        var parseTreeNode = leaf.Parent;
        if (parseTreeNode == null)
            return false;

        SyntaxRule_Cur = null;
        ParseNode_Cur = null;
        ErrorToken = null;
        ErrorMessage = null;
        SyntaxRule_Err = null;
        ParseNode_Err = null;

        //tokens = lines[leaf.Line].tokens;

        //currentLine = leaf.Line;
        //currentTokenIndex = leaf.TokenIndex;
        m_kTokenScanner.MoveTo(leaf.Line, leaf.TokenIndex);
        m_kTokenScanner.MoveNext();

        SyntaxRule_Cur = leaf.Parent;
        ErrorMessage = null;
        Seeking = true;
        ParseNode_Err = ParseNode_Cur = leaf.ParseNode.parent.NextAfterChild(leaf.ParseNode, this);
        Seeking = false;
        SyntaxRule_Err = SyntaxRule_Cur;
        return true;
    }
}
