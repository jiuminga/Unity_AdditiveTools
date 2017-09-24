using System.Collections.Generic;
using System.Text;

public static class LRHelper_UnityShell
{
    public static FormatedLine[] Lex(List<string> lsLine)
    {
        if (lsLine.Count == 0) return new FormatedLine[0];
        var lexer = Lexer_UnityShell.Instacnce;
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
        return new SyntaxTreeBuilder_UnityShell(new TokenScanner(lsFormatedLine), cus).Build();
    }

    public static void SysmbolResolve(FormatedLine[] lsFormatedLine)
    {
        var kScanner = new TokenScanner(lsFormatedLine);
        while (kScanner.MoveNext())
        {
            SymbolResolver.ResolveNode(kScanner.Current.m_kLinkedLeaf.Parent);
        }
    }
}
