using System;
using System.Collections.Generic;
public class ParseNode_Many : ParseNode_Base
{
    protected readonly ParseNode_Base node;

    public ParseNode_Many(ParseNode_Base node)
    {
        var idNode = node as ParseNode_Id;
        if (idNode != null)
            node = idNode.Clone();

        node.parent = this;
        this.node = node;
    }

    public override ParseNode_Base GetNode()
    {
        if (node is ParseNode_Many_Opt)    // [{ [ n ] }] -> [{ n }]
            return new ParseNode_Many(node.GetNode());

        if (node is ParseNode_Many)   // [{ [{ n }] }] -> [{ n }]
            return node;

        return this;
    }

    // lookahead includes empty.
    public override TokenSet Init_PreCheckSet(ParseNode_Root parser)
    {
        if (FirstSet == null)
        {
            FirstSet = new TokenSet(node.Init_PreCheckSet(parser));
            FirstSet.AddEmpty();
        }
        return FirstSet;
    }

    // subtree gets succ.

    public override TokenSet Init_FollowSet(ParseNode_Root parser, TokenSet succ)
    {
        Init_PreCheckSet(parser);
        FollowSet = succ;
        node.Init_FollowSet(parser, succ);
        return FirstSet;
    }

    // subtree is checked.
    public override void CheckLL1(ParseNode_Root parser)
    {
        // trust the predicate!
        //base.CheckLL1(parser);
        if (FollowSet == null)
            throw new Exception(this + ": follow not set");
        node.CheckLL1(parser);
    }

    public override bool Matches(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        return node.Matches(pSyntaxTreeBuilder);
    }

    public override bool Scan(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        if (!pSyntaxTreeBuilder.KeepScanning)
            return true;

        var ifNode = node as ParseNode_ManyOpt_If ?? node as ParseNode_ManyOpt_IfNot;
        if (ifNode != null)
        {
            int tokenIndex, line;
            do
            {
                tokenIndex = pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex;
                line = pSyntaxTreeBuilder.TokenScanner.CurrentLine - 1;
                if (!node.Scan(pSyntaxTreeBuilder))
                    return false;
                if (!pSyntaxTreeBuilder.KeepScanning)
                    return true;
            } while (pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex != tokenIndex || pSyntaxTreeBuilder.TokenScanner.CurrentLine - 1 != line);
        }
        else
        {
            while (FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
            {
                int tokenIndex = pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex;
                int line = pSyntaxTreeBuilder.TokenScanner.CurrentLine;
                if (!node.Scan(pSyntaxTreeBuilder))
                    return false;
                if (!pSyntaxTreeBuilder.KeepScanning)
                    return true;
                if (pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex == tokenIndex && pSyntaxTreeBuilder.TokenScanner.CurrentLine - 1 == line)
                    throw new Exception("Infinite loop!!!");
            }
        }
        return true;
    }

    public override ParseNode_Base NextAfterChild(ParseNode_Base child, SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        //	if (pSyntaxTreeBuilder.ErrorMessage == null || Parse(pSyntaxTreeBuilder.Clone()))
        return this;

        //	return base.NextAfterChild(child, pSyntaxTreeBuilder);
    }

    public override ParseNode_Base Parse(SyntaxTreeBuilder pSyntaxTreeBuilder)
    {
        var ifNode = node as ParseNode_ManyOpt_If;
        if (ifNode != null)
        {
            if (!ifNode.Matches(pSyntaxTreeBuilder))
                return parent.NextAfterChild(this, pSyntaxTreeBuilder);

            var tokenIndex = pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex;
            var line = pSyntaxTreeBuilder.TokenScanner.CurrentLine;
            var nextNode = node.Parse(pSyntaxTreeBuilder);
            if (nextNode != this || pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex != tokenIndex || pSyntaxTreeBuilder.TokenScanner.CurrentLine != line)
                return nextNode;
            //Debug.Log("Exiting Many " + this + " in goal: " + pSyntaxTreeBuilder.CurrentParseTreeNode);
        }
        else
        {
            if (!FirstSet.Contains(pSyntaxTreeBuilder.TokenScanner.Current.tokenId))
                return parent.NextAfterChild(this, pSyntaxTreeBuilder);

            var tokenIndex = pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex;
            var line = pSyntaxTreeBuilder.TokenScanner.CurrentLine;
            var nextNode = node.Parse(pSyntaxTreeBuilder);
            if (!(nextNode == this && pSyntaxTreeBuilder.TokenScanner.CurrentTokenIndex == tokenIndex && pSyntaxTreeBuilder.TokenScanner.CurrentLine == line))
                // throw new Exception("Infinite loop!!! while parsing " + pSyntaxTreeBuilder.Current + " on line " + pSyntaxTreeBuilder.CurrentLine());
                return nextNode;
        }
        return parent.NextAfterChild(this, pSyntaxTreeBuilder);
    }

    public override String ToString()
    {
        return "[{ " + node + " }]";
    }

    public sealed override IEnumerable<ParseNode_Lit> EnumerateLitNodes()
    {
        foreach (var n in node.EnumerateLitNodes())
            yield return n;
    }

    public sealed override IEnumerable<ParseNode_Id> EnumerateIdNodes()
    {
        foreach (var n in node.EnumerateIdNodes())
            yield return n;
    }

    public override IEnumerable<T> EnumerateNodesOfType<T>()
    {
        foreach (var n in node.EnumerateNodesOfType<T>())
            yield return n;
        base.EnumerateNodesOfType<T>();
    }
}
