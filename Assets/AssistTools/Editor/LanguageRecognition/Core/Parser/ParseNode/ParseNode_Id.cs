using UnityEngine;
using System.Collections.Generic;
public class ParseNode_Id : ParseNode_Base
{
    public readonly string Name;

    // Token or Rule.
    public ParseNode_Base LinkedTarget { get; protected set; }

    public ParseNode_Rule Rule { get { return LinkedTarget as ParseNode_Rule; } }

    public ParseNode_Id(string name)
    {
        this.Name = name;
    }

    public ParseNode_Id Clone()
    {
        var clone = new ParseNode_Id(Name) { LinkedTarget = LinkedTarget, FirstSet = FirstSet, FollowSet = FollowSet };
        var token = LinkedTarget as ParseNode_Token;
        if (token != null)
        {
            clone.LinkedTarget = token.Clone();
            clone.LinkedTarget.parent = this;
        }
        return clone;
    }

    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
        {
            LinkedTarget = parser.GetRuleOrTakenCopy(Name);
            if (LinkedTarget == null)
                Debug.LogError("Parser rule \"" + Name + "\" not found!!!");
            else
            {
                LinkedTarget.parent = this;
                LinkedTarget.childIndex = 0;
                FirstSet = LinkedTarget.Init_PreCheckSet(parser);
            }
        }
        return FirstSet;
    }

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        Init_PreCheckSet(parser);
        if (LinkedTarget is ParseNode_Rule)
            LinkedTarget.Init_FollowSet(parser, succ);

        return FirstSet;
    }

    public override void CheckLL1(ParseNode_Root parser) { }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return !pSyntaxTreeBuilder.KeepScanning || LinkedTarget.Scan(pSyntaxTreeBuilder);
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        LinkedTarget.parent = this;
        var rule = LinkedTarget as ParseNode_Rule;
        if (rule != null)
        {
            bool skip;
            pSyntaxTreeBuilder.SyntaxRule_Cur = pSyntaxTreeBuilder.SyntaxRule_Cur.AddNode(this, pSyntaxTreeBuilder, out skip);
            if (skip)
                return pSyntaxTreeBuilder.ParseNode_Cur;
        }
        var result2 = LinkedTarget.Parse(pSyntaxTreeBuilder);
        LinkedTarget.parent = this;
        return result2;
    }

    public override ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        try
        {
            if (LinkedTarget is ParseNode_Rule)
                pSyntaxTreeBuilder.SyntaxRule_Cur = pSyntaxTreeBuilder.SyntaxRule_Cur.Parent;
            return base.NextAfterChild(this, pSyntaxTreeBuilder);
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public override string ToString()
    {
        return Name;
    }

    public sealed override IEnumerable<ParseNode_Id> EnumerateIdNodes()
    {
        yield return this;
    }
}
