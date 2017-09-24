using System;
using System.Collections.Generic;
using System.Text;

public class ParseNode_Root : ParseNode_Base
{
    private static string cachedErrorMessage;
    private static ParseNode_Base cachedErrorParseNode;

    private readonly List<ParseNode_Rule> m_lsRule = new List<ParseNode_Rule>();
    public ParseNode_Rule Rule_Start { get { return m_lsRule[0]; } }

    private readonly Dictionary<string, ParseNode_Rule> m_dicNt2Rule = new Dictionary<string, ParseNode_Rule>();

    protected Dictionary<string, ParseNode_Base> m_dicIdName2RuleOrToken = new Dictionary<string, ParseNode_Base>();

    public string[] m_lsToken;
    public ParseNode_Id RootID;

    public void AddRule(ParseNode_Rule rule)
    {
        var nt = rule.NtName;
        if (m_dicNt2Rule.ContainsKey(nt))
            throw new Exception(nt + ": duplicate");
        m_dicNt2Rule.Add(nt, rule);
        m_lsRule.Add(rule);
    }

    public void InitSyntax()
    {
        var setLits = new HashSet<string>();
        var lsLit_and_ID = new List<String>();

        foreach (ParseNode_Lit lit in EnumerateLitNodes())
        {
            if (setLits.Contains(lit.body))
                continue;

            setLits.Add(lit.body);
            lsLit_and_ID.Add(lit.body);
        }

        foreach (var id in EnumerateIdNodes())
        {
            var idName = id.Name;
            if (m_dicIdName2RuleOrToken.ContainsKey(idName))
                continue;

            m_dicIdName2RuleOrToken.Add(idName, m_dicNt2Rule.ContainsKey(idName) ? m_dicNt2Rule[idName] : null);
            lsLit_and_ID.Add(idName);
        }

        lsLit_and_ID.Sort();
        m_lsToken = lsLit_and_ID.ToArray();
        for (var i = 0; i < m_lsToken.Length; ++i)
        {
            var name = m_lsToken[i];
            if (!setLits.Contains(name) && m_dicIdName2RuleOrToken[name] == null)
            {
                m_dicIdName2RuleOrToken[name] = new ParseNode_Token(name, new TokenSet(i));

                if (name == "NAME")
                {
                    m_dicIdName2RuleOrToken[name].FirstSet.Add(m_dicIdName2RuleOrToken["IDENTIFIER"].FirstSet);
                }
            }
        }

        Init_PreCheckSet(this);
        Init_FollowSet(this, null);

        CheckLL1(this);
    }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        foreach (var rule in m_lsRule)
        {
            rule.Init_PreCheckSet(this);
        }
        return null;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {

        Rule_Start._InitFollowSet_();
        bool followChanged;
        do
        {
            foreach (var rule in m_lsRule)
                rule._InitFollowSet_(this);
            followChanged = false;
            foreach (var rule in m_lsRule)
                followChanged |= rule.FollowChanged();
        } while (followChanged);
        return null;
    }

    public override void CheckLL1(ParseNode_Root parser)
    {
        foreach (var rule in m_lsRule)
            rule.CheckLL1(this);
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        throw new InvalidOperationException();
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        throw new InvalidOperationException();
    }

    public void Init()
    {
        RootID = new ParseNode_Id(Rule_Start.NtName);
        m_dicIdName2RuleOrToken[Rule_Start.NtName] = Rule_Start;
        RootID.Init_PreCheckSet(this);
        Rule_Start.parent = RootID;
    }
    #region Parse
    public LR_SyntaxTree ParseAll(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.TokenScanner.MoveNext())
            return null;

        var kSyntaxTree = new LR_SyntaxTree();
        var rootId = new ParseNode_Id(Rule_Start.NtName);
        m_dicIdName2RuleOrToken[Rule_Start.NtName] = Rule_Start;
        rootId.Init_PreCheckSet(this);
        Rule_Start.parent = rootId;
        pSyntaxTreeBuilder.SyntaxRule_Cur = kSyntaxTree.root = new SyntaxTreeNode_Rule(rootId);
        pSyntaxTreeBuilder.ParseNode_Cur = Rule_Start.Parse(pSyntaxTreeBuilder);

        pSyntaxTreeBuilder.SyntaxRule_Err = pSyntaxTreeBuilder.SyntaxRule_Cur;
        pSyntaxTreeBuilder.ParseNode_Err = pSyntaxTreeBuilder.ParseNode_Cur;

        while (pSyntaxTreeBuilder.ParseNode_Cur != null)
            if (!ParseStep(pSyntaxTreeBuilder))
                break;

        return kSyntaxTree;
    }

    public bool ParseStep(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (pSyntaxTreeBuilder.ParseNode_Cur == null)
            return false;

        var token = pSyntaxTreeBuilder.TokenScanner.Current;
        if (pSyntaxTreeBuilder.ErrorMessage == null)
        {
            while (pSyntaxTreeBuilder.ParseNode_Cur != null)
            {
                pSyntaxTreeBuilder.ParseNode_Cur = pSyntaxTreeBuilder.ParseNode_Cur.Parse(pSyntaxTreeBuilder);
                if (pSyntaxTreeBuilder.ErrorMessage != null || token != pSyntaxTreeBuilder.TokenScanner.Current)
                    break;
            }

            if (pSyntaxTreeBuilder.ErrorMessage == null && token != pSyntaxTreeBuilder.TokenScanner.Current)
            {
                pSyntaxTreeBuilder.SyntaxRule_Err = pSyntaxTreeBuilder.SyntaxRule_Cur;
                pSyntaxTreeBuilder.ParseNode_Err = pSyntaxTreeBuilder.ParseNode_Cur;
            }
        }
        if (pSyntaxTreeBuilder.ErrorMessage != null)
        {
            if (token.tokenKind == LexerToken.Kind.EOF)
            {
                return false;
            }

            var missingParseTreeNode = pSyntaxTreeBuilder.SyntaxRule_Cur;
            var missingParseNode = pSyntaxTreeBuilder.ParseNode_Cur;

            pSyntaxTreeBuilder.SyntaxRule_Cur = pSyntaxTreeBuilder.SyntaxRule_Err;
            pSyntaxTreeBuilder.ParseNode_Cur = pSyntaxTreeBuilder.ParseNode_Err;
            if (pSyntaxTreeBuilder.SyntaxRule_Cur != null)
            {
                var cpt = pSyntaxTreeBuilder.SyntaxRule_Cur;
                for (var i = cpt.NumValidNodes; i > 0 && !cpt.ChildAt(--i).HasLeafs();)
                    cpt.InvalidateFrom(i);
            }

            if (pSyntaxTreeBuilder.ParseNode_Cur != null)
            {
                int numSkipped;
                pSyntaxTreeBuilder.ParseNode_Cur = pSyntaxTreeBuilder.ParseNode_Cur.Recover(pSyntaxTreeBuilder, out numSkipped);
            }
            if (pSyntaxTreeBuilder.ParseNode_Cur == null)
            {
                if (token.m_kLinkedLeaf != null)
                    token.m_kLinkedLeaf.ReparseToken();
                new SyntaxTreeNode_Leaf(pSyntaxTreeBuilder.TokenScanner);

                if (cachedErrorParseNode == pSyntaxTreeBuilder.ParseNode_Err)
                {
                    token.m_kLinkedLeaf.m_sSyntaxError = cachedErrorMessage;
                }
                else
                {
                    token.m_kLinkedLeaf.m_sSyntaxError = "Unexpected token! Expected " + pSyntaxTreeBuilder.ParseNode_Err.FirstSet.ToString(this);
                    cachedErrorMessage = token.m_kLinkedLeaf.m_sSyntaxError;
                    cachedErrorParseNode = pSyntaxTreeBuilder.ParseNode_Err;
                }

                pSyntaxTreeBuilder.ParseNode_Cur = pSyntaxTreeBuilder.ParseNode_Err;
                pSyntaxTreeBuilder.SyntaxRule_Cur = pSyntaxTreeBuilder.SyntaxRule_Err;

                if (!pSyntaxTreeBuilder.TokenScanner.MoveNext())
                {
                    return false;
                }
                pSyntaxTreeBuilder.ErrorMessage = null;
            }
            else
            {
                if (missingParseNode != null && missingParseTreeNode != null)
                {
                    pSyntaxTreeBuilder.SyntaxRule_Cur = missingParseTreeNode;
                    pSyntaxTreeBuilder.ParseNode_Cur = missingParseNode;
                }

                pSyntaxTreeBuilder.InsertMissingToken(pSyntaxTreeBuilder.ErrorMessage
                    ?? ("Expected " + missingParseNode.FirstSet.ToString(this)));

                if (missingParseNode != null && missingParseTreeNode != null)
                {
                    pSyntaxTreeBuilder.ErrorMessage = null;
                    pSyntaxTreeBuilder.ErrorToken = null;
                    pSyntaxTreeBuilder.SyntaxRule_Cur = missingParseTreeNode;
                    pSyntaxTreeBuilder.ParseNode_Cur = missingParseNode;
                    pSyntaxTreeBuilder.ParseNode_Cur = missingParseNode.parent.NextAfterChild(missingParseNode, pSyntaxTreeBuilder);
                }
                pSyntaxTreeBuilder.ErrorMessage = null;
                pSyntaxTreeBuilder.ErrorToken = null;
            }
        }

        return true;
    }
    #endregion

    public override string ToString()
    {
        var s = new StringBuilder(GetType().Name + " {\n");
        foreach (var rule in m_lsRule)
            s.AppendLine(rule.ToString(this));
        s.Append("}");
        return s.ToString();
    }

    public int TokenToId(string s)
    {
        return Array.BinarySearch<string>(m_lsToken, s);
    }

    public string GetToken(int tokenId)
    {
        return tokenId >= 0 && tokenId < m_lsToken.Length ? m_lsToken[tokenId] : tokenId + "?";
    }

    public ParseNode_Base GetRuleOrTakenCopy(string name)
    {
        var ret = m_dicIdName2RuleOrToken[name];
        var token = ret as ParseNode_Token;
        if (token != null)
            ret = token.Clone();
        return ret;
    }

    public sealed override IEnumerable<ParseNode_Lit> EnumerateLitNodes()
    {
        foreach (var rule in m_lsRule)
            foreach (var node in rule.EnumerateLitNodes())
                yield return node;
    }

    public sealed override IEnumerable<ParseNode_Id> EnumerateIdNodes()
    {
        foreach (var rule in m_lsRule)
            foreach (var node in rule.EnumerateIdNodes())
                yield return node;
    }

    public override IEnumerable<T> EnumerateNodesOfType<T>()
    {
        foreach (var rule in m_lsRule)
            foreach (var node in rule.EnumerateNodesOfType<T>())
                yield return node;
        base.EnumerateNodesOfType<T>();
    }
}
