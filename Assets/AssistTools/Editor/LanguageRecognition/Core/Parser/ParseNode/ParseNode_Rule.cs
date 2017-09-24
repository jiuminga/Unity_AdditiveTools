using System;
using System.Collections.Generic;
using System.Text;
public class ParseNode_Rule : ParseNode_Base
{
    public static bool debug;
    public SemanticFlags semantics;
    public bool autoExclude;
    public bool contextualKeyword;

    protected string m_sNtName;
    public string NtName
    {
        get { return m_sNtName; }
    }

    protected ParseNode_Base rhs;
    protected bool followChanged;
    protected bool inProgress;

    public ParseNode_Rule(string nt, ParseNode_Base rhs)
    {
        var idNode = rhs as ParseNode_Id;
        if (idNode != null)
            rhs = idNode.Clone();
        this.m_sNtName = nt;
        rhs.parent = this;
        this.rhs = rhs;
    }

    public bool FollowChanged()
    {
        inProgress = followChanged;
        followChanged = false;
        return inProgress;
    }

    public void _InitFollowSet_()
    {
        FollowSet = new TokenSet();
    }

    public void _InitFollowSet_(ParseNode_Root parser)
    {
        if (FirstSet == null)
            throw new Exception(m_sNtName + ": lookahead not set");
        if (FollowSet == null)
            return;// throw new Exception(nt + ": not connected");
        if (inProgress)
            rhs.Init_FollowSet(parser, FollowSet);
    }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
        {
            if (inProgress)
                throw new Exception(m_sNtName + ": recursive lookahead");
            inProgress = true;
            FirstSet = rhs.Init_PreCheckSet(parser);
        }
        return FirstSet;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        if (FollowSet == null)
        {
            followChanged = true;
            FollowSet = new TokenSet(succ);
        }
        else if (FollowSet.Add(succ))
        {
            followChanged = true;
        }
        return FirstSet;
    }

    public override void CheckLL1(ParseNode_Root parser)
    {
        if (!contextualKeyword)
        {
            base.CheckLL1(parser);
            rhs.CheckLL1(parser);
        }
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;
        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current))
            return rhs.Scan(pSyntaxTreeBuilder);
        else if (!FirstSet.ContainsEmpty())
            return false;
        return true;
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        bool wasError = pSyntaxTreeBuilder.ErrorMessage != null;
        ParseNode_Base ret = null;
        if (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current))
        {
            ret = rhs.Parse(pSyntaxTreeBuilder);
        }
        if ((ret == null || !wasError && pSyntaxTreeBuilder.ErrorMessage != null) && !FirstSet.ContainsEmpty())
        {
            pSyntaxTreeBuilder.SyntaxErrorExpected(FirstSet);
            return ret ?? this;
        }
        if (ret != null)
            return ret;
        return NextAfterChild(rhs, pSyntaxTreeBuilder); // ready to be reduced
    }

    public override ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        var SyntaxRule_Cur = pSyntaxTreeBuilder.SyntaxRule_Cur;
        if (SyntaxRule_Cur == null)
            return null;
        var res = SyntaxRule_Cur.ParseNode != null ? SyntaxRule_Cur.ParseNode.NextAfterChild(this, pSyntaxTreeBuilder) : null;

        if (pSyntaxTreeBuilder.Seeking)
            return res;

        if (contextualKeyword && SyntaxRule_Cur.NumValidNodes == 1)
        {
            var token = SyntaxRule_Cur.LeafAt(0).token;
            token.tokenKind = LexerToken.Kind.ContextualKeyword;
        }
        if (SyntaxRule_Cur.Semantics != SemanticFlags.None)
        {
            pSyntaxTreeBuilder.OnSemanticNodeClose(SyntaxRule_Cur);
        }
        return res;
    }

    public override string ToString()
    {
        return m_sNtName + " : " + rhs + " .";
    }

    public string ToString(ParseNode_Root parser)
    {
        var result = new StringBuilder(m_sNtName + " : " + rhs + " .");
        if (FirstSet != null)
            result.Append("\n  lookahead " + FirstSet.ToString(parser));
        if (FollowSet != null)
            result.Append("\n  follow " + FollowSet.ToString(parser));
        return result.ToString();
    }

    public sealed override IEnumerable<ParseNode_Lit> EnumerateLitNodes()
    {
        foreach (var node in rhs.EnumerateLitNodes())
            yield return node;
    }

    public sealed override IEnumerable<ParseNode_Id> EnumerateIdNodes()
    {
        foreach (var node in rhs.EnumerateIdNodes())
            yield return node;
    }

    public override IEnumerable<T> EnumerateNodesOfType<T>()
    {
        foreach (var node in rhs.EnumerateNodesOfType<T>())
            yield return node;
        base.EnumerateNodesOfType<T>();
    }
}