using System;
using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

public class Lexer_UnityShell : Lexer_Base
{
    private static Lexer_UnityShell m_kInstance;
    public static Lexer_UnityShell Instacnce { get { return m_kInstance ?? new Lexer_UnityShell(); } }
    private Lexer_UnityShell() { m_kInstance = this; }

    public override HashSet<string> Keywords { get { return keywords; } }
    public override HashSet<string> BuiltInLiterals { get { return scriptLiterals; } }
    public override HashSet<string> CompilationDefines
    {
        get
        {
            if (m_setCompilationDefines == null)
                m_setCompilationDefines = new HashSet<string>(UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines);
            return m_setCompilationDefines;
        }
    }
    private HashSet<string> m_setCompilationDefines;

    private static readonly HashSet<string> keywords = new HashSet<string>{
        "abstract", "as", "base", "break", "case", "catch", "checked", "class", "const", "continue",
        "default", "delegate", "do", "else", "enum", "event", "explicit", "extern", "finally",
        "fixed", "for", "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is",
        "lock", "namespace", "new", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sealed", "sizeof", "stackalloc", "static",
        "struct", "switch", "this", "throw", "try", "typeof", "unchecked", "unsafe", "using", "virtual",
        "volatile", "while"
    };

    private static readonly HashSet<string> csOperators = new HashSet<string>{
        "++", "--", "->", "+", "-", "!", "~", "++", "--", "&", "*", "/", "%", "+", "-", "<<", ">>", "<", ">",
        "<=", ">=", "==", "!=", "&", "^", "|", "&&", "||", "??", "?", "::", ":",
        "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "=>"
    };

    private static readonly HashSet<string> builtInTypes = new HashSet<string>{
        "bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short",
        "string", "uint", "ulong", "ushort", "void"
    };

    public override void Tokenize(string sData, FormatedLine formatedLine)
    {
        var tokens = formatedLine.tokens = new List<LexerToken>();

        int iIndex = 0;
        int iLength = sData.Length;
        LexerToken token;

        TryScan_Whitespace(sData, ref iIndex, formatedLine);

        while (iIndex < iLength)
        {
            switch (formatedLine.blockState)
            {
                case FormatedLine.BlockState.None:
                    if (TryScan_Whitespace(sData, ref iIndex, formatedLine)) continue;

                    if (formatedLine.regionTree.kind > RegionTree.Kind.LastActive)
                    {
                        tokens.Add(new LexerToken(LexerToken.Kind.Comment, sData.Substring(iIndex)) { formatedLine = formatedLine });
                        iIndex = iLength;
                        break;
                    }

                    if (sData[iIndex] == '/' && iIndex < iLength - 1)
                    {
                        if (sData[iIndex + 1] == '/')
                        {
                            tokens.Add(new LexerToken(LexerToken.Kind.Comment, "//") { formatedLine = formatedLine });
                            iIndex += 2;
                            tokens.Add(new LexerToken(LexerToken.Kind.Comment, sData.Substring(iIndex)) { formatedLine = formatedLine });
                            iIndex = iLength;
                            break;
                        }
                        else if (sData[iIndex + 1] == '*')
                        {
                            tokens.Add(new LexerToken(LexerToken.Kind.Comment, "/*") { formatedLine = formatedLine });
                            iIndex += 2;
                            formatedLine.blockState = FormatedLine.BlockState.CommentBlock;
                            break;
                        }
                    }

                    if (sData[iIndex] == '\'')
                    {
                        token = ScanCharLiteral(sData, ref iIndex);
                        tokens.Add(token);
                        token.formatedLine = formatedLine;
                        break;
                    }

                    if (sData[iIndex] == '\"')
                    {
                        token = ScanStringLiteral(sData, ref iIndex);
                        tokens.Add(token);
                        token.formatedLine = formatedLine;
                        break;
                    }

                    if (iIndex < iLength - 1 && sData[iIndex] == '@' && sData[iIndex + 1] == '\"')
                    {
                        token = new LexerToken(LexerToken.Kind.VerbatimStringBegin, sData.Substring(iIndex, 2)) { formatedLine = formatedLine };
                        tokens.Add(token);
                        iIndex += 2;
                        formatedLine.blockState = FormatedLine.BlockState.StringBlock;
                        break;
                    }

                    if (sData[iIndex] >= '0' && sData[iIndex] <= '9'
                        || iIndex < iLength - 1 && sData[iIndex] == '.' && sData[iIndex + 1] >= '0' && sData[iIndex + 1] <= '9')
                    {
                        token = ScanNumericLiteral(sData, ref iIndex);
                        tokens.Add(token);
                        token.formatedLine = formatedLine;
                        break;
                    }

                    token = ScanIdentifierOrKeyword(sData, ref iIndex);
                    if (token != null)
                    {
                        tokens.Add(token);
                        token.formatedLine = formatedLine;
                        break;
                    }

                    // Multi-character operators / punctuators
                    // "++", "--", "<<", ">>", "<=", ">=", "==", "!=", "&&", "||", "??", "+=", "-=", "*=", "/=", "%=",
                    // "&=", "|=", "^=", "<<=", ">>=", "=>", "::"
                    var punctuatorStart = iIndex++;
                    if (iIndex < sData.Length)
                    {
                        switch (sData[punctuatorStart])
                        {
                            case '?':
                                if (sData[iIndex] == '?')
                                    ++iIndex;
                                break;
                            case '+':
                                if (sData[iIndex] == '+' || sData[iIndex] == '=')
                                    ++iIndex;
                                break;
                            case '-':
                                if (sData[iIndex] == '-' || sData[iIndex] == '=')
                                    ++iIndex;
                                break;
                            case '<':
                                if (sData[iIndex] == '=')
                                    ++iIndex;
                                else if (sData[iIndex] == '<')
                                {
                                    ++iIndex;
                                    if (iIndex < sData.Length && sData[iIndex] == '=')
                                        ++iIndex;
                                }
                                break;
                            case '>':
                                if (sData[iIndex] == '=')
                                    ++iIndex;
                                break;
                            case '=':
                                if (sData[iIndex] == '=' || sData[iIndex] == '>')
                                    ++iIndex;
                                break;
                            case '&':
                                if (sData[iIndex] == '=' || sData[iIndex] == '&')
                                    ++iIndex;
                                break;
                            case '|':
                                if (sData[iIndex] == '=' || sData[iIndex] == '|')
                                    ++iIndex;
                                break;
                            case '*':
                            case '/':
                            case '%':
                            case '^':
                            case '!':
                                if (sData[iIndex] == '=')
                                    ++iIndex;
                                break;
                            case ':':
                                if (sData[iIndex] == ':')
                                    ++iIndex;
                                break;
                        }
                    }
                    tokens.Add(new LexerToken(LexerToken.Kind.Punctuator, sData.Substring(punctuatorStart, iIndex - punctuatorStart)) { formatedLine = formatedLine });
                    break;

                case FormatedLine.BlockState.CommentBlock:
                    int commentBlockEnd = sData.IndexOf("*/", iIndex, StringComparison.Ordinal);
                    if (commentBlockEnd == -1)
                    {
                        tokens.Add(new LexerToken(LexerToken.Kind.Comment, sData.Substring(iIndex)) { formatedLine = formatedLine });
                        iIndex = iLength;
                    }
                    else
                    {
                        tokens.Add(new LexerToken(LexerToken.Kind.Comment, sData.Substring(iIndex, commentBlockEnd + 2 - iIndex)) { formatedLine = formatedLine });
                        iIndex = commentBlockEnd + 2;
                        formatedLine.blockState = FormatedLine.BlockState.None;
                    }
                    break;

                case FormatedLine.BlockState.StringBlock:
                    int i = iIndex;
                    int closingQuote = sData.IndexOf('\"', iIndex);
                    while (closingQuote != -1 && closingQuote < iLength - 1 && sData[closingQuote + 1] == '\"')
                    {
                        i = closingQuote + 2;
                        closingQuote = sData.IndexOf('\"', i);
                    }
                    if (closingQuote == -1)
                    {
                        tokens.Add(new LexerToken(LexerToken.Kind.VerbatimStringLiteral, sData.Substring(iIndex)) { formatedLine = formatedLine });
                        iIndex = iLength;
                    }
                    else
                    {
                        tokens.Add(new LexerToken(LexerToken.Kind.VerbatimStringLiteral, sData.Substring(iIndex, closingQuote - iIndex)) { formatedLine = formatedLine });
                        iIndex = closingQuote;
                        tokens.Add(new LexerToken(LexerToken.Kind.VerbatimStringLiteral, sData.Substring(iIndex, 1)) { formatedLine = formatedLine });
                        ++iIndex;
                        formatedLine.blockState = FormatedLine.BlockState.None;
                    }
                    break;
            }
        }
    }

    private bool IsBuiltInType(string word)
    {
        return builtInTypes.Contains(word);
    }

    private bool IsKeyword(string word)
    {
        return keywords.Contains(word);
    }

    private bool IsOperator(string text)
    {
        return csOperators.Contains(text);
    }

    private new LexerToken ScanIdentifierOrKeyword(string line, ref int startAt)
    {
        var token = Lexer_Base.ScanIdentifierOrKeyword(line, ref startAt);
        if (token != null && token.tokenKind == LexerToken.Kind.Keyword && !IsKeyword(token.text) && !IsBuiltInType(token.text))
            token.tokenKind = LexerToken.Kind.Identifier;
        return token;
    }

}
