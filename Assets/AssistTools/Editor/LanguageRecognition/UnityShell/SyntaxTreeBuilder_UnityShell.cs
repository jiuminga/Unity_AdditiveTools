using System;
using System.Collections.Generic;
using System.Linq;

public class SyntaxTreeBuilder_UnityShell : SyntaxTreeBuilder
{
    private Parser_UnityShell m_kParser = Parser_UnityShell.Instance;
    public override Parser_Base Parser { get { return m_kParser; } }

    public void OnTokanMoveAt(LexerToken token)
    {
        switch (token.tokenKind)
        {
            case LexerToken.Kind.Missing:
            case LexerToken.Kind.Whitespace:
            case LexerToken.Kind.Comment:
            case LexerToken.Kind.EOF:
            case LexerToken.Kind.Preprocessor:
            case LexerToken.Kind.PreprocessorSymbol:
            case LexerToken.Kind.PreprocessorArguments:
            case LexerToken.Kind.PreprocessorDirectiveExpected:
            case LexerToken.Kind.PreprocessorCommentExpected:
            case LexerToken.Kind.PreprocessorUnexpectedDirective:
            case LexerToken.Kind.VerbatimStringLiteral:
                break;
            case LexerToken.Kind.Punctuator:
            case LexerToken.Kind.Keyword:
            case LexerToken.Kind.BuiltInLiteral:
                token.tokenId = Parser.TokenToId(token.text);
                break;
            case LexerToken.Kind.Identifier:
            case LexerToken.Kind.ContextualKeyword:
                token.tokenId = m_kParser.tokenIdentifier;
                break;
            case LexerToken.Kind.IntegerLiteral:
            case LexerToken.Kind.RealLiteral:
            case LexerToken.Kind.CharLiteral:
            case LexerToken.Kind.StringLiteral:
            case LexerToken.Kind.VerbatimStringBegin:
                token.tokenId = m_kParser.tokenLiteral;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public SyntaxTreeBuilder_UnityShell(TokenScanner tokenScanner, Scope_CompilationUnit pComplilationUnitScop)
    {
        m_kTokenScanner = tokenScanner;
        ComplilationUnitScop = pComplilationUnitScop;
        tokenScanner.Reset();
        tokenScanner.OnTokenMoveAt = OnTokanMoveAt;
        tokenScanner.EOF = new LexerToken(LexerToken.Kind.EOF, string.Empty) { tokenId = m_kParser.tokenEOF };

        //lines = formatedLines;
        //if (EOF == null)
        //    EOF = new LexerToken(LexerToken.Kind.EOF, string.Empty) { tokenId = parser.tokenEOF };
    }

    public override SyntaxTreeBuilder Clone()
    {
        var clone = new SyntaxTreeBuilder_UnityShell(m_kTokenScanner, ComplilationUnitScop)
        {
            //tokens = tokens,
            //currentLine = currentLine,
            //currentTokenIndex = currentTokenIndex,
            //currentTokenCache = currentTokenCache,
            ParseNode_Cur = ParseNode_Cur,
            SyntaxRule_Cur = SyntaxRule_Cur,
            ErrorToken = ErrorToken,
            ErrorMessage = ErrorMessage,
            ParseNode_Err = ParseNode_Err,
            SyntaxRule_Err = SyntaxRule_Err,
            m_kTokenScanner = m_kTokenScanner.Clone()
        };
        return clone;
    }

    public override void OnSemanticNodeClose(SyntaxTreeNode_Rule node)
    {
        m_kParser.OnSemanticNodeClose(this, node);
    }

    public override void SyntaxErrorExpected(TokenSet lookahead)
    {
        if (ErrorMessage != null)
            return;

        ErrorMessage = "Syntax error: Expected " + lookahead.ToString(Parser.ParseRoot);
        if (SyntaxRule_Cur != null && SyntaxRule_Cur.m_sSyntaxError == null)
        {
            SyntaxRule_Cur.m_sSyntaxError = ErrorMessage;
        }
    }

    public override bool CollectCompletions(TokenSet tokenSet)
    {
        var result = (ParseNode_Cur ?? Parser_CSharp.Instance.ParseRoot.Rule_Start).CollectCompletions(tokenSet, this, m_kParser.tokenIdentifier);
        if (ParseNode_Cur != null && tokenSet.Contains(Parser_CSharp.Instance.tokenIdentifier))
        {
            //TODO:unknown Code
            //var currentTokenCache = new LexerToken(LexerToken.Kind.Identifier, "special") { tokenId = m_kParser.tokenIdentifier };
            //currentTokenCache.tokenId = Parser.tokenIdentifier;
            PreCheck(ParseNode_Cur, 1);
            //currentTokenCache = null;
        }
        return result;
    }

    public override void InsertMissingToken(string errorMessage)
    {
        var missingAtLine = m_kTokenScanner.CurrentLine;
        var missingAtIndex = m_kTokenScanner.CurrentTokenIndex;
        while (true)
        {
            if (--missingAtIndex < 0)
            {
                if (--missingAtLine < 0)
                {
                    missingAtLine = missingAtIndex = 0;
                    break;
                }
                missingAtIndex = m_kTokenScanner.GetFormatedLine(missingAtLine).tokens.Count;
                continue;
            }
            var tokenKind = m_kTokenScanner.GetFormatedLine(missingAtLine).tokens[missingAtIndex].tokenKind;
            if (tokenKind > LexerToken.Kind.LastWSToken)
            {
                ++missingAtIndex;
                break;
            }
            else if (tokenKind == LexerToken.Kind.Missing)
            {
                ErrorToken = m_kTokenScanner.GetFormatedLine(missingAtLine).tokens[missingAtIndex].m_kLinkedLeaf;
                return;
            }
        }

        var missingLine = m_kTokenScanner.GetFormatedLine(missingAtLine);
        var missingToken = new LexerToken(LexerToken.Kind.Missing, string.Empty) { style = null, formatedLine = missingLine };
        //missingLine.tokens.Insert(missingAtIndex, missingToken);
        var leaf = SyntaxRule_Err.AddToken(missingToken);
        leaf.m_bMissing = true;
        leaf.m_sSyntaxError = errorMessage;
        leaf.ParseNode = ParseNode_Err;

        if (ErrorToken == null)
            ErrorToken = leaf;

        m_kTokenScanner.InsertToken(missingToken, missingAtLine, missingAtIndex);
        //if (missingAtLine == currentLine)
        //    ++currentTokenIndex;
    }

    public void MoveToLine(int line, LR_SyntaxTree parseTree)
    {
        for (var prevLine = line - 1; prevLine >= 0; --prevLine)
        {
            var tokens = m_kTokenScanner.GetFormatedLine(prevLine).tokens;
            for (var i = tokens.Count - 1; i >= 0; --i)
            {
                var token = tokens[i];
                var leaf = token.m_kLinkedLeaf;
                if (token.tokenKind == LexerToken.Kind.Missing)
                {
                    if (token.m_kLinkedLeaf != null && token.m_kLinkedLeaf.Parent != null)
                        token.m_kLinkedLeaf.Parent.m_sSyntaxError = null;
                    tokens.RemoveAt(i);
                    continue;
                }

                if (leaf == null || leaf.ParseNode == null)
                    continue;

                if (token.tokenKind < LexerToken.Kind.LastWSToken)
                    continue;

                if (leaf.m_sSyntaxError != null)
                {
                    ErrorToken = leaf;
                    ErrorMessage = leaf.m_sSyntaxError;
                    continue;
                }

                MoveAfterLeaf(leaf);
                return;
            }
        }

        //tokens = null;
        //currentLine = -1;
        //currentTokenIndex = -1;
        m_kTokenScanner.Reset();

        SyntaxRule_Cur = null;
        ParseNode_Cur = null;
        ErrorToken = null;
        ErrorMessage = null;
        SyntaxRule_Err = null;
        ParseNode_Err = null;

        m_kTokenScanner.MoveNext();

        ParseNode_Rule startRule = Parser_CSharp.Instance.ParseRoot.Rule_Start;
        SyntaxRule_Cur = parseTree.root;// = new ParseTree.Node(rootId);
        ParseNode_Cur = startRule;//.Parse(pSyntaxTreeBuilder);
        SyntaxRule_Err = SyntaxRule_Cur;
        ParseNode_Err = ParseNode_Cur;
    }

    public int maxScanDistance;
    public override bool KeepScanning
    {
        get { return maxScanDistance > 0; }
    }

    public override bool PreCheck(ParseNode_Base node, int maxDistance = int.MaxValue)
    {
        if (m_kTokenScanner.IsEnding)
            return false;
        var line = m_kTokenScanner.CurrentLine;
        var index = m_kTokenScanner.CurrentTokenIndex;
        //	var realIndex = nonTriviaTokenIndex;

        var temp = maxScanDistance;
        maxScanDistance = maxDistance;
        var match = node.Scan(this);
        maxScanDistance = temp;

        //for (var i = m_kTokenScanner.CurrentLine; i > line; --i)
        //    if (i < lines.Length)
        //        lines[i].laLines = Math.Max(lines[i].laLines, i - line);

        m_kTokenScanner.MoveTo(line, index);
        //m_kTokenScanner.CurrentLine = line;
        //m_kTokenScanner.CurrentTokenIndex = index;
        //	nonTriviaTokenIndex = realIndex;
        //tokens = currentLine < lines.Length ? lines[currentLine].tokens : null;
        return match;
    }

    public override LexerToken Lookahead(int offset, bool skipWhitespace = true)
    {
        //if (!skipWhitespace)
        //{
        //return m_kTokenScanner.CurrentTokenIndex + 1 < tokens.Count ? tokens[currentTokenIndex + 1] : EOF;
        //}
        //var t = tokens;
        var line = m_kTokenScanner.CurrentLine;
        var index = m_kTokenScanner.CurrentTokenIndex;

        while (offset-- > 0)
        {
            if (!m_kTokenScanner.MoveNext())
            {
                m_kTokenScanner.MoveTo(line, index);
                return m_kTokenScanner.EOF;
            }
        }
        var token = m_kTokenScanner.Current;// tokens[currentTokenIndex];

        //for (var i = m_kTokenScanner.CurrentLine; i > line; --i)
        //    if (i < lines.Length)
        //        lines[i].laLines = Math.Max(lines[i].laLines, i - line);

        //tokens = t;
        //currentLine = cl;
        //currentTokenIndex = cti;
        m_kTokenScanner.MoveTo(line, index);
        return token;
    }
}

