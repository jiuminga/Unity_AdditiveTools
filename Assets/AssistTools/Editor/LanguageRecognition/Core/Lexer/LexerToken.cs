using UnityEngine;

public class LexerToken //: IComparable<SyntaxToken>
{
    public enum Kind
    {
        Missing,
        Whitespace,
        Comment,//注释
        Preprocessor,
        PreprocessorArguments,
        PreprocessorSymbol,
        PreprocessorDirectiveExpected,
        PreprocessorCommentExpected,
        PreprocessorUnexpectedDirective,
        VerbatimStringLiteral,//字面上字符串

        LastWSToken, // Marker only

        VerbatimStringBegin,
        BuiltInLiteral,
        CharLiteral,
        StringLiteral,
        IntegerLiteral,
        RealLiteral,
        Punctuator,//标点
        Keyword,
        Identifier,
        ContextualKeyword,
        EOF,
    }

    public Kind tokenKind;
    public GUIStyle style;
    public SyntaxTreeNode_Leaf m_kLinkedLeaf;
    //public TextSpan textSpan;

    public string text;
    public int tokenId;

    public FormatedLine formatedLine;

    public int Line { get { return formatedLine.index; } }
    public int TokenIndex { get { return formatedLine.tokens.IndexOf(this); } }

    public static LexerToken CreateMissing()
    {
        return new LexerToken(Kind.Missing, string.Empty) { m_kLinkedLeaf = null };
    }

    public LexerToken(Kind kind, string text)
    {
        m_kLinkedLeaf = null;
        tokenKind = kind;
        this.text = string.Intern(text);
        tokenId = -1;
        style = null;
    }

    public bool IsMissing()
    {
        return tokenKind == Kind.Missing;
    }

    public override string ToString()
    {
        return /*string.Format("(<color=#ffff00ff>{0}</color>:<b>{1}</b>)", tokenKind, text);*/ tokenKind + "(\"" + text + "\")";
    }

    public string Dump() { return "[Token: " + tokenKind + " \"" + text + "\"]"; }
}


