using UnityEngine;
using System;
using System.Collections.Generic;
public abstract class ParseNode_Base
{
    public ParseNode_Base parent;
    public int childIndex;

    public TokenSet FirstSet;
    public TokenSet FollowSet;

    public static implicit operator ParseNode_Base(string s)
    {
        return new ParseNode_Lit(s);
    }

    public static ParseNode_Base operator |(ParseNode_Base a, ParseNode_Base b)
    {
        return new ParseNode_Alt(a, b);
    }

    public static ParseNode_Base operator |(ParseNode_Alt a, ParseNode_Base b)
    {
        a.Add(b);
        return a;
    }

    public static ParseNode_Base operator -(ParseNode_Base a, ParseNode_Base b)
    {
        return new ParseNode_Seq(a, b);
    }

    public static ParseNode_Base operator -(ParseNode_Seq a, ParseNode_Base b)
    {
        a.Add(b);
        return a;
    }

    #region Template
    public abstract TokenSet Init_PreCheckSet(ParseNode_Root parser);
    public abstract TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ);
    public abstract bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder);
    public abstract ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder);

    public virtual ParseNode_Base GetNode()
    {
        return this;
    }

    public virtual void Add(ParseNode_Base node)
    {
        throw new Exception(GetType() + ": cannot Add()");
    }

    public virtual bool Matches(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current);
    }

    public virtual void CheckLL1(ParseNode_Root parser)
    {
        if (FollowSet == null)
            throw new Exception(this + ": follow not set");
        if (FirstSet.ContainsEmpty() && FirstSet.Accepts(FollowSet))
            throw new Exception(this + ": ambiguous\n"
                + "  lookahead " + FirstSet.ToString(parser) + "\n"
                + "  follow " + FollowSet.ToString(parser));
    }

    public virtual IEnumerable<ParseNode_Lit> EnumerateLitNodes() { yield break; }

    public virtual IEnumerable<ParseNode_Id> EnumerateIdNodes() { yield break; }

    public virtual IEnumerable<T> EnumerateNodesOfType<T>() where T : ParseNode_Base
    {
        if (this is T)
            yield return (T)this;
    }

    public virtual ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return parent != null ? parent.NextAfterChild(this, pSyntaxTreeBuilder) : null;
    }
    #endregion

    public void SyntaxError(SyntaxTreeBuilder pSyntaxTreeBuilder, string errorMessage)
    {
        if (pSyntaxTreeBuilder.ErrorMessage != null)
            return;
        pSyntaxTreeBuilder.ErrorMessage = errorMessage;
    }


    public ParseNode_Base Recover(SyntaxTreeBuilder pSyntaxTreeBuilder, out int numMissing)
    {
        numMissing = 0;

        var current = this;
        while (current.parent != null)
        {
            var next = current.parent.NextAfterChild(current, pSyntaxTreeBuilder);
            if (next == null)
                break;

            var nextId = next as ParseNode_Id;
            if (nextId != null && nextId.Name == "attribute")
                return nextId;

            var nextMatchespSyntaxTreeBuilder = next.Matches(pSyntaxTreeBuilder);
            while (next != null && !nextMatchespSyntaxTreeBuilder && next.FirstSet.ContainsEmpty())
            {
                next = next.parent.NextAfterChild(next, pSyntaxTreeBuilder);
                nextMatchespSyntaxTreeBuilder = next != null && next.Matches(pSyntaxTreeBuilder);
            }

            if (nextMatchespSyntaxTreeBuilder && pSyntaxTreeBuilder.TokenScanner.Current.text == ";" && next is ParseNode_Many_Opt)
            {
                return null;
            }

            ++numMissing;
            if (nextMatchespSyntaxTreeBuilder)
            {
                if (pSyntaxTreeBuilder.TokenScanner.Current.text == "{" ||
                    pSyntaxTreeBuilder.TokenScanner.Current.text == "}" ||
                    pSyntaxTreeBuilder.PreCheck(next, 3))//next.Scan(clone))
                {
                    return next;
                }
            }

            if (numMissing <= 1 && pSyntaxTreeBuilder.TokenScanner.Current.text != "{" && pSyntaxTreeBuilder.TokenScanner.Current.text != "}")
            {
                //TODO using
                var lapSyntaxTreeBuilder = pSyntaxTreeBuilder.Clone();
                if (lapSyntaxTreeBuilder.TokenScanner.MoveNext() && next.Matches(lapSyntaxTreeBuilder) && lapSyntaxTreeBuilder.PreCheck(next, 3))
                {
                    return null;
                }
            }
            current = next;
        }
        return null;
    }

    public bool CollectCompletions(TokenSet tokenSet, SyntaxTreeBuilder pSyntaxTreeBuilder, int identifierId)
    {
        var clone = pSyntaxTreeBuilder.Clone();

        var hasName = false;
        var current = this;
        while (current != null && current.parent != null)
        {
            tokenSet.Add(current.FirstSet);

            if (current.FirstSet.Contains(identifierId))
            {
                ParseNode_Rule currentRule = null;
                var currentId = current as ParseNode_Id;
                if (currentId == null)
                {
                    var rule = current.parent;
                    while (rule != null && !(rule is ParseNode_Rule))
                        rule = rule.parent;
                    currentId = rule != null ? rule.parent as ParseNode_Id : null;
                    currentRule = rule as ParseNode_Rule;
                }

                if (currentId != null)
                {
                    var peerAsRule = currentId.LinkedTarget as ParseNode_Rule;
                    if (peerAsRule != null && peerAsRule.contextualKeyword)
                    {
                        Debug.Log(currentId.Name);
                    }
                    else if (currentRule != null && currentRule.contextualKeyword)
                    {
                        Debug.Log("currentRule " + currentRule.NtName);
                    }
                    else
                    {
                        var id = currentId.Name;
                        if (Array.IndexOf(new[]
                            {
                                    "constantDeclarators",
                                    "constantDeclarator",
                                }, id) >= 0)
                        {
                            hasName = true;
                        }
                    }
                }
            }

            if (!current.FirstSet.ContainsEmpty())
                break;

            current = current.parent.NextAfterChild(current, clone);
        }
        tokenSet.RemoveEmpty();

        return hasName;
    }
}
