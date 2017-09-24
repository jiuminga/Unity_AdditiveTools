using System;
using System.Collections.Generic;


[UnityEditor.InitializeOnLoad]
public abstract class Lexer_Base
{
    protected static readonly char[] whitespaces = { ' ', '\t' };
    protected static readonly HashSet<string> scriptLiterals = new HashSet<string> { "false", "null", "true", };

    private static readonly HashSet<string> EmptyStringArray = new HashSet<string>();
    public bool scriptDefinesChanged;

    public virtual HashSet<string> Keywords { get { return EmptyStringArray; } }
    public virtual HashSet<string> BuiltInLiterals { get { return EmptyStringArray; } }
    public virtual HashSet<string> CompilationDefines { get { return new HashSet<string>(); } }


    public virtual void LexLine(string textLine, FormatedLine formatedLine)
    {
        var lineTokens = formatedLine.tokens = new List<LexerToken>();

        if (textLine.Length != 0)
        {
            lineTokens.Add(new LexerToken(LexerToken.Kind.Comment, textLine) { /*style = textBuffer.styles.normalStyle,*/ formatedLine = formatedLine });
        }
    }

    public virtual void Tokenize(string line, FormatedLine formatedLine) { }

    #region Scan
    protected bool TryScan_Whitespace(string sData, ref int iIndex, FormatedLine formatedLine)
    {
        int i = iIndex;
        while (i < sData.Length && (sData[i] == ' ' || sData[i] == '\t'))
            ++i;
        if (i == iIndex)
            return false;

        formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.Whitespace, sData.Substring(iIndex, i - iIndex)) { formatedLine = formatedLine });
        iIndex = i;
        return true;
    }

    protected bool TryScan_PreprocessSymbol(string sData, ref int iIndex, FormatedLine formatedLine)
    {
        if (formatedLine.blockState == FormatedLine.BlockState.None && iIndex < sData.Length && sData[iIndex] == '#')
        {
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.Preprocessor, "#") { formatedLine = formatedLine });
            ++iIndex;
            return true;
        }
        return false;
    }

    protected static LexerToken ScanWhitespace(string line, ref int startAt)
    {
        int i = startAt;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            ++i;
        if (i == startAt)
            return null;

        var token = new LexerToken(LexerToken.Kind.Whitespace, line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static LexerToken ScanWord(string line, ref int startAt)
    {
        int i = startAt;
        while (i < line.Length)
        {
            if (!Char.IsLetterOrDigit(line, i) && line[i] != '_')
                break;
            ++i;
        }
        var token = new LexerToken(LexerToken.Kind.Identifier, line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static bool ScanUnicodeEscapeChar(string line, ref int startAt)
    {
        if (startAt >= line.Length - 5)
            return false;
        if (line[startAt] != '\\')
            return false;
        int i = startAt + 1;
        if (line[i] != 'u' && line[i] != 'U')
            return false;
        var n = line[i] == 'u' ? 4 : 8;
        ++i;
        while (n > 0)
        {
            if (!ScanHexDigit(line, ref i))
                break;
            --n;
        }
        if (n == 0)
        {
            startAt = i;
            return true;
        }
        return false;
    }

    protected static LexerToken ScanCharLiteral(string line, ref int startAt)
    {
        var i = startAt + 1;
        while (i < line.Length)
        {
            if (line[i] == '\'')
            {
                ++i;
                break;
            }
            if (line[i] == '\\' && i < line.Length - 1)
                ++i;
            ++i;
        }
        var token = new LexerToken(LexerToken.Kind.CharLiteral, line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static LexerToken ScanStringLiteral(string line, ref int startAt)
    {
        var i = startAt + 1;
        while (i < line.Length)
        {
            if (line[i] == '\"')
            {
                ++i;
                break;
            }
            if (line[i] == '\\' && i < line.Length - 1)
                ++i;
            ++i;
        }
        var token = new LexerToken(LexerToken.Kind.StringLiteral, line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static LexerToken ScanNumericLiteral(string line, ref int startAt)
    {
        bool hex = false;
        bool point = false;
        bool exponent = false;
        var i = startAt;

        LexerToken token;

        char c;
        if (line[i] == '0' && i < line.Length - 1 && (line[i + 1] == 'x' || line[i + 1] == 'X'))
        {
            i += 2;
            hex = true;
            while (i < line.Length)
            {
                c = line[i];
                if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')
                    ++i;
                else
                    break;
            }
        }
        else
        {
            while (i < line.Length && line[i] >= '0' && line[i] <= '9')
                ++i;
        }

        if (i > startAt && i < line.Length)
        {
            c = line[i];
            if (c == 'l' || c == 'L' || c == 'u' || c == 'U')
            {
                ++i;
                if (i < line.Length)
                {
                    if (c == 'l' || c == 'L')
                    {
                        if (line[i] == 'u' || line[i] == 'U')
                            ++i;
                    }
                    else if (line[i] == 'l' || line[i] == 'L')
                        ++i;
                }
                token = new LexerToken(LexerToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
                startAt = i;
                return token;
            }
        }

        if (hex)
        {
            token = new LexerToken(LexerToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
            startAt = i;
            return token;
        }

        while (i < line.Length)
        {
            c = line[i];

            if (!point && !exponent && c == '.')
            {
                if (i < line.Length - 1 && line[i + 1] >= '0' && line[i + 1] <= '9')
                {
                    point = true;
                    ++i;
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (!exponent && i > startAt && (c == 'e' || c == 'E'))
            {
                exponent = true;
                ++i;
                if (i < line.Length && (line[i] == '-' || line[i] == '+'))
                    ++i;
                continue;
            }
            if (c == 'f' || c == 'F' || c == 'd' || c == 'D' || c == 'm' || c == 'M')
            {
                point = true;
                ++i;
                break;
            }
            if (c < '0' || c > '9')
                break;
            ++i;
        }
        token = new LexerToken(
            point || exponent ? LexerToken.Kind.RealLiteral : LexerToken.Kind.IntegerLiteral,
            line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static LexerToken ScanNumericLiteral_JS(string line, ref int startAt)
    {
        bool hex = false;
        bool point = false;
        bool exponent = false;
        var i = startAt;

        LexerToken token;

        char c;
        if (line[i] == '0' && i < line.Length - 1 && (line[i + 1] == 'x'))
        {
            i += 2;
            hex = true;
            while (i < line.Length)
            {
                c = line[i];
                if (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F')
                    ++i;
                else
                    break;
            }
        }
        else
        {
            while (i < line.Length && line[i] >= '0' && line[i] <= '9')
                ++i;
        }

        if (i > startAt && i < line.Length)
        {
            c = line[i];
            if (c == 'l' || c == 'L')
            {
                ++i;
                token = new LexerToken(LexerToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
                startAt = i;
                return token;
            }
        }

        if (hex)
        {
            token = new LexerToken(LexerToken.Kind.IntegerLiteral, line.Substring(startAt, i - startAt));
            startAt = i;
            return token;
        }

        while (i < line.Length)
        {
            c = line[i];

            if (!point && !exponent && c == '.')
            {
                if (i < line.Length - 1 && line[i + 1] >= '0' && line[i + 1] <= '9')
                {
                    point = true;
                    ++i;
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (!exponent && i > startAt && (c == 'e' || c == 'E'))
            {
                exponent = true;
                ++i;
                if (i < line.Length && (line[i] == '-' || line[i] == '+'))
                    ++i;
                continue;
            }
            if (c == 'f' || c == 'F' || c == 'd' || c == 'D')
            {
                point = true;
                ++i;
                break;
            }
            if (c < '0' || c > '9')
                break;
            ++i;
        }
        token = new LexerToken(
            point || exponent ? LexerToken.Kind.RealLiteral : LexerToken.Kind.IntegerLiteral,
            line.Substring(startAt, i - startAt));
        startAt = i;
        return token;
    }

    protected static bool ScanHexDigit(string line, ref int i)
    {
        if (i >= line.Length)
            return false;
        char c = line[i];
        if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f')
        {
            ++i;
            return true;
        }
        return false;
    }

    protected static LexerToken ScanIdentifierOrKeyword(string line, ref int startAt)
    {
        bool identifier = false;
        int i = startAt;
        if (i >= line.Length)
            return null;

        char c = line[i];
        if (c == '@')
        {
            identifier = true;
            ++i;
        }
        if (i < line.Length)
        {
            c = line[i];
            if (char.IsLetter(c) || c == '_')
            {
                ++i;
            }
            else if (!ScanUnicodeEscapeChar(line, ref i))
            {
                if (i == startAt)
                    return null;
                var partialWord = line.Substring(startAt, i - startAt);
                startAt = i;
                return new LexerToken(LexerToken.Kind.Identifier, partialWord);
            }
            else
            {
                identifier = true;
            }

            while (i < line.Length)
            {
                if (char.IsLetterOrDigit(line, i) || line[i] == '_')
                    ++i;
                else if (!ScanUnicodeEscapeChar(line, ref i))
                    break;
                else
                    identifier = true;
            }
        }

        var word = line.Substring(startAt, i - startAt);
        startAt = i;
        return new LexerToken(identifier ? LexerToken.Kind.Identifier : LexerToken.Kind.Keyword, word);
    }
    #endregion

    #region Pre
    protected bool ParsePPOrExpression(string line, FormatedLine formatedLine, ref int startAt)
    {
        if (startAt >= line.Length)
        {
            //TODO: Insert missing token
            return true;
        }

        var lhs = ParsePPAndExpression(line, formatedLine, ref startAt);

        var ws = ScanWhitespace(line, ref startAt);
        if (ws != null)
        {
            formatedLine.tokens.Add(ws);
            ws.formatedLine = formatedLine;
        }

        if (startAt + 1 < line.Length && line[startAt] == '|' && line[startAt + 1] == '|')
        {
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, "||") { formatedLine = formatedLine });
            startAt += 2;

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            var rhs = ParsePPOrExpression(line, formatedLine, ref startAt);

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            return lhs || rhs;
        }

        return lhs;
    }

    protected bool ParsePPAndExpression(string line, FormatedLine formatedLine, ref int startAt)
    {
        if (startAt >= line.Length)
        {
            //TODO: Insert missing token
            return true;
        }

        var lhs = ParsePPEqualityExpression(line, formatedLine, ref startAt);

        var ws = ScanWhitespace(line, ref startAt);
        if (ws != null)
        {
            formatedLine.tokens.Add(ws);
            ws.formatedLine = formatedLine;
        }

        if (startAt + 1 < line.Length && line[startAt] == '&' && line[startAt + 1] == '&')
        {
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, "&&") { formatedLine = formatedLine });
            startAt += 2;

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            var rhs = ParsePPAndExpression(line, formatedLine, ref startAt);

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            return lhs && rhs;
        }

        return lhs;
    }

    protected bool ParsePPEqualityExpression(string line, FormatedLine formatedLine, ref int startAt)
    {
        if (startAt >= line.Length)
        {
            //TODO: Insert missing token
            return true;
        }

        var lhs = ParsePPUnaryExpression(line, formatedLine, ref startAt);

        var ws = ScanWhitespace(line, ref startAt);
        if (ws != null)
        {
            formatedLine.tokens.Add(ws);
            ws.formatedLine = formatedLine;
        }

        if (startAt + 1 < line.Length && (line[startAt] == '=' || line[startAt + 1] == '!') && line[startAt + 1] == '=')
        {
            var equality = line[startAt] == '=';
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, equality ? "==" : "!=") { formatedLine = formatedLine });
            startAt += 2;

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            var rhs = ParsePPEqualityExpression(line, formatedLine, ref startAt);

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            return equality ? lhs == rhs : lhs != rhs;
        }

        return lhs;
    }

    protected bool ParsePPUnaryExpression(string line, FormatedLine formatedLine, ref int startAt)
    {
        if (startAt >= line.Length)
        {
            //TODO: Insert missing token
            return true;
        }

        if (line[startAt] == '!')
        {
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, "!") { formatedLine = formatedLine });
            ++startAt;

            var ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            var result = ParsePPUnaryExpression(line, formatedLine, ref startAt);

            ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            return !result;
        }

        return ParsePPPrimaryExpression(line, formatedLine, ref startAt);
    }

    protected bool ParsePPPrimaryExpression(string line, FormatedLine formatedLine, ref int startAt)
    {
        if (line[startAt] == '(')
        {
            formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, "(") { formatedLine = formatedLine });
            ++startAt;

            var ws = ScanWhitespace(line, ref startAt);
            if (ws != null)
            {
                formatedLine.tokens.Add(ws);
                ws.formatedLine = formatedLine;
            }

            var result = ParsePPOrExpression(line, formatedLine, ref startAt);

            if (startAt >= line.Length)
            {
                //TODO: Insert missing token
                return result;
            }

            if (line[startAt] == ')')
            {
                formatedLine.tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, ")") { formatedLine = formatedLine });
                ++startAt;

                ws = ScanWhitespace(line, ref startAt);
                if (ws != null)
                {
                    formatedLine.tokens.Add(ws);
                    ws.formatedLine = formatedLine;
                }

                return result;
            }

            //TODO: Insert missing token
            return result;
        }

        var symbolResult = ParsePPSymbol(line, formatedLine, ref startAt);

        var ws2 = ScanWhitespace(line, ref startAt);
        if (ws2 != null)
        {
            formatedLine.tokens.Add(ws2);
            ws2.formatedLine = formatedLine;
        }

        return symbolResult;
    }

    protected bool ParsePPSymbol(string line, FormatedLine formatedLine, ref int startAt)
    {
        var word = Lexer_Base.ScanIdentifierOrKeyword(line, ref startAt);
        if (word == null)
            return true;

        word.tokenKind = LexerToken.Kind.PreprocessorSymbol;
        formatedLine.tokens.Add(word);
        word.formatedLine = formatedLine;

        if (word.text == "true")
        {
            return true;
        }
        if (word.text == "false")
        {
            return false;
        }

        var isDefined = CompilationDefines.Contains(word.text);
        return isDefined;
    }

    protected void OpenRegion(FormatedLine formatedLine, RegionTree.Kind regionKind)
    {
        var parentRegion = formatedLine.regionTree;
        RegionTree reuseRegion = null;

        switch (regionKind)
        {
            case RegionTree.Kind.Else:
            case RegionTree.Kind.Elif:
            case RegionTree.Kind.InactiveElse:
            case RegionTree.Kind.InactiveElif:
                parentRegion = parentRegion.parent;
                break;
        }

        if (parentRegion.children != null)
        {
            reuseRegion = parentRegion.children.Find(x => x.line == formatedLine);
        }
        if (reuseRegion != null)
        {
            if (reuseRegion.kind == regionKind)
            {
                formatedLine.regionTree = reuseRegion;
                return;
            }

            reuseRegion.parent = null;
            parentRegion.children.Remove(reuseRegion);
        }

        formatedLine.regionTree = new RegionTree
        {
            parent = parentRegion,
            kind = regionKind,
            line = formatedLine,
        };

        if (parentRegion.children == null)
            parentRegion.children = new List<RegionTree>();
        parentRegion.children.Add(formatedLine.regionTree);
    }

    protected void CloseRegion(FormatedLine formatedLine)
    {
        formatedLine.regionTree = formatedLine.regionTree.parent;
    }
    #endregion
}
