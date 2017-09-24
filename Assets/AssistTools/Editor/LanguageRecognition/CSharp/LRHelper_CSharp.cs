using System.Collections.Generic;
using System.Text;

public static class LRHelper_CSharp
{
    public static FormatedLine[] Lex(List<string> lsLine)
    {
        if (lsLine.Count == 0) return new FormatedLine[0];
        var lexer = new Lexer_CSharp();
        var ret = new FormatedLine[lsLine.Count];
        var prev = ret[0] = new FormatedLine() { index = 0, regionTree = new RegionTree(), blockState = FormatedLine.BlockState.None };
        for (int i = 0; i < lsLine.Count; ++i)
        {
            prev = ret[i] = new FormatedLine() { index = i, regionTree = prev.regionTree, blockState = prev.blockState };
            lexer.Tokenize(lsLine[i], ret[i]);
        }
        return ret;
    }

    public static LR_SyntaxTree Parse(FormatedLine[] lsFormatedLine, Scope_CompilationUnit cus)
    {
        return new SyntaxTreeBuilder_CSharp(new TokenScanner(lsFormatedLine), cus).Build();
    }

    public static void SysmbolResolve(FormatedLine[] lsFormatedLine)
    {
        var kScanner = new TokenScanner(lsFormatedLine);
        while (kScanner.MoveNext())
        {
            SymbolResolver.ResolveNode(kScanner.Current.m_kLinkedLeaf.Parent);
        }
        //if (token.tokenKind == LexerToken.Kind.ContextualKeyword)
        //{
        //    tokenStyle = token.text == "value" ? textBuffer.styles.parameterStyle : textBuffer.styles.keywordStyle;
        //    if (token.text == "var" && token.m_kLinkedLeaf != null && (token.m_kLinkedLeaf.ResolvedSymbol == null || token.m_kLinkedLeaf.ResolvedSymbol.kind == SymbolKind.Error))
        //        SymbolResolver.ResolveNode(token.m_kLinkedLeaf.Parent);
        //    return tokenStyle;
        //}

        //var leaf = token.m_kLinkedLeaf;
        //if (leaf != null && leaf.Parent != null)
        //{
        //    if (token.tokenKind == LexerToken.Kind.Keyword)
        //    {
        //        if ((token.text == "base" || token.text == "this") && (leaf.ResolvedSymbol == null || leaf.m_sSyntaxError != null))
        //            SymbolResolver.ResolveNode(leaf.Parent);
        //    }
        //    else if (token.tokenKind == LexerToken.Kind.Identifier)
        //    {
        //        if (leaf.ResolvedSymbol == null || leaf.m_sSyntaxError != null)
        //            SymbolResolver.ResolveNode(leaf.Parent);

        //    }
        //}
    }
}
