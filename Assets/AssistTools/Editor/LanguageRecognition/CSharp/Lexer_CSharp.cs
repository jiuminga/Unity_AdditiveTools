using System;
using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

public class Lexer_CSharp : Lexer_Base
{
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

    private static readonly HashSet<string> preprocessorKeywords = new HashSet<string>{
        "define", "elif", "else", "endif", "endregion", "error", "if", "line", "pragma", "region", "undef", "warning"
    };

    private static readonly HashSet<string> builtInTypes = new HashSet<string>{
        "bool", "byte", "char", "decimal", "double", "float", "int", "long", "object", "sbyte", "short",
        "string", "uint", "ulong", "ushort", "void"
    };

    protected HashSet<string> m_setTypes;
    protected void InitTypes()
    {
        m_setTypes = new HashSet<string>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                if (assembly is System.Reflection.Emit.AssemblyBuilder)
                    continue;

                var takeAllTypes = SD_Assembly.IsScriptAssemblyName(assembly.GetName().Name);
                var assemblyTypes = takeAllTypes ? assembly.GetTypes() : assembly.GetExportedTypes();
                foreach (var type in assemblyTypes)
                {
                    var name = type.Name;
                    var index = name.IndexOf('`');
                    if (index >= 0)
                        name = name.Remove(index);
                    m_setTypes.Add(name);
                    if (type.IsSubclassOf(typeof(Attribute)) && name.EndsWith("Attribute", StringComparison.Ordinal))
                        m_setTypes.Add(type.Name.Substring(0, type.Name.Length - "Attribute".Length));
                }
            }
            catch (ReflectionTypeLoadException)
            {
                Debug.LogWarning("Error reading types from assembly " + assembly.FullName);
            }
        }
    }

    public override void Tokenize(string sData, FormatedLine formatedLine)
    {
        var tokens = formatedLine.tokens = new List<LexerToken>();

        int iIndex = 0;
        int iLength = sData.Length;
        LexerToken token;

        TryScan_Whitespace(sData, ref iIndex, formatedLine);

        if (TryScan_PreprocessSymbol(sData, ref iIndex, formatedLine))
        {
            Preprocess(sData, ref iIndex, formatedLine);
            return;
        }

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

    private void Preprocess(string sData, ref int iIndex, FormatedLine formatedLine)
    {
        TryScan_Whitespace(sData, ref iIndex, formatedLine);

        var error = false;
        var commentsOnly = false;

        int iLength = sData.Length;
        List<LexerToken> tokens = formatedLine.tokens;
        LexerToken token = ScanWord(sData, ref iIndex);
        if (!preprocessorKeywords.Contains(token.text))
        {
            token.tokenKind = LexerToken.Kind.PreprocessorDirectiveExpected;
            tokens.Add(token);
            token.formatedLine = formatedLine;
            error = true;
        }
        else
        {
            token.tokenKind = LexerToken.Kind.Preprocessor;
            tokens.Add(token);
            token.formatedLine = formatedLine;
            TryScan_Whitespace(sData, ref iIndex, formatedLine);

            switch (token.text)
            {
                case "if":
                    if (ParsePPOrExpression(sData, formatedLine, ref iIndex) && formatedLine.regionTree.kind <= RegionTree.Kind.LastActive)
                        OpenRegion(formatedLine, RegionTree.Kind.If);
                    else OpenRegion(formatedLine, RegionTree.Kind.InactiveIf);
                    commentsOnly = true;
                    break;

                case "elif":
                    bool active = ParsePPOrExpression(sData, formatedLine, ref iIndex);
                    switch (formatedLine.regionTree.kind)
                    {
                        case RegionTree.Kind.If:
                        case RegionTree.Kind.Elif:
                        case RegionTree.Kind.InactiveElif:
                            OpenRegion(formatedLine, RegionTree.Kind.InactiveElif); break;
                        case RegionTree.Kind.InactiveIf:
                            if (active && formatedLine.regionTree.kind <= RegionTree.Kind.LastActive)
                                OpenRegion(formatedLine, RegionTree.Kind.Elif);
                            else OpenRegion(formatedLine, RegionTree.Kind.InactiveElif);
                            break;
                        default:
                            token.tokenKind = LexerToken.Kind.PreprocessorUnexpectedDirective; break;
                    }
                    break;

                case "else":
                    if (formatedLine.regionTree.kind == RegionTree.Kind.If ||
                        formatedLine.regionTree.kind == RegionTree.Kind.Elif)
                        OpenRegion(formatedLine, RegionTree.Kind.InactiveElse);
                    else if (formatedLine.regionTree.kind == RegionTree.Kind.InactiveIf ||
                        formatedLine.regionTree.kind == RegionTree.Kind.InactiveElif)
                    {
                        if (formatedLine.regionTree.parent.kind > RegionTree.Kind.LastActive)
                            OpenRegion(formatedLine, RegionTree.Kind.InactiveElse);
                        else OpenRegion(formatedLine, RegionTree.Kind.Else);
                    }
                    else token.tokenKind = LexerToken.Kind.PreprocessorUnexpectedDirective;
                    break;

                case "endif":
                    if (formatedLine.regionTree.kind == RegionTree.Kind.If ||
                        formatedLine.regionTree.kind == RegionTree.Kind.Elif ||
                        formatedLine.regionTree.kind == RegionTree.Kind.Else ||
                        formatedLine.regionTree.kind == RegionTree.Kind.InactiveIf ||
                        formatedLine.regionTree.kind == RegionTree.Kind.InactiveElif ||
                        formatedLine.regionTree.kind == RegionTree.Kind.InactiveElse)
                        CloseRegion(formatedLine);
                    else token.tokenKind = LexerToken.Kind.PreprocessorUnexpectedDirective;
                    break;

                case "define":
                case "undef":
                    {
                        var symbol = Lexer_Base.ScanIdentifierOrKeyword(sData, ref iIndex);
                        if (symbol != null && symbol.text != "true" && symbol.text != "false")
                        {
                            symbol.tokenKind = LexerToken.Kind.PreprocessorSymbol;
                            formatedLine.tokens.Add(symbol);
                            symbol.formatedLine = formatedLine;
                            scriptDefinesChanged = true;

                            var inactive = formatedLine.regionTree.kind > RegionTree.Kind.LastActive;
                            if (!inactive)
                            {
                                if (token.text == "define")
                                {
                                    if (!CompilationDefines.Contains(symbol.text))
                                        CompilationDefines.Add(symbol.text);
                                }
                                else if (CompilationDefines.Contains(symbol.text)) CompilationDefines.Remove(symbol.text);
                            }
                        }
                    }
                    break;


                case "region":
                    if (formatedLine.regionTree.kind > RegionTree.Kind.LastActive)
                        OpenRegion(formatedLine, RegionTree.Kind.InactiveRegion);
                    else OpenRegion(formatedLine, RegionTree.Kind.Region);
                    break;

                case "endregion":
                    if (formatedLine.regionTree.kind == RegionTree.Kind.Region ||
                        formatedLine.regionTree.kind == RegionTree.Kind.InactiveRegion)
                        CloseRegion(formatedLine);
                    else token.tokenKind = LexerToken.Kind.PreprocessorUnexpectedDirective;
                    break;

                case "error":
                case "warning":
                    break;
            }
        }

        switch (token.text)
        {
            case "region":
            case "endregion":
            case "error":
            case "warning":
                TryScan_Whitespace(sData, ref iIndex, formatedLine);
                if (iIndex < iLength)
                {
                    var textArgument = sData.Substring(iIndex);
                    textArgument.TrimEnd(new[] { ' ', '\t' });
                    tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorArguments, textArgument) { formatedLine = formatedLine });
                    iIndex = iLength - textArgument.Length;
                    if (iIndex < iLength)
                        tokens.Add(new LexerToken(LexerToken.Kind.Whitespace, sData.Substring(iIndex)) { formatedLine = formatedLine });
                }
                return;
        }

        while (iIndex < iLength)
        {
            if (TryScan_Whitespace(sData, ref iIndex, formatedLine)) continue;

            var firstChar = sData[iIndex];
            if (iIndex < iLength - 1 && firstChar == '/' && sData[iIndex + 1] == '/')
            {
                tokens.Add(new LexerToken(LexerToken.Kind.Comment, sData.Substring(iIndex)) { formatedLine = formatedLine });
                break;
            }
            else if (commentsOnly)
            {
                tokens.Add(new LexerToken(LexerToken.Kind.PreprocessorCommentExpected, sData.Substring(iIndex)) { formatedLine = formatedLine });
                break;
            }

            if (char.IsLetterOrDigit(firstChar) || firstChar == '_')
            {
                token = ScanWord(sData, ref iIndex);
                token.tokenKind = LexerToken.Kind.PreprocessorArguments;
                tokens.Add(token);
                token.formatedLine = formatedLine;
            }
            else if (firstChar == '"')
            {
                token = ScanStringLiteral(sData, ref iIndex);
                token.tokenKind = LexerToken.Kind.PreprocessorArguments;
                tokens.Add(token);
                token.formatedLine = formatedLine;
            }
            else if (firstChar == '\'')
            {
                token = ScanCharLiteral(sData, ref iIndex);
                token.tokenKind = LexerToken.Kind.PreprocessorArguments;
                tokens.Add(token);
                token.formatedLine = formatedLine;
            }
            else
            {
                token = new LexerToken(LexerToken.Kind.PreprocessorArguments, firstChar.ToString()) { formatedLine = formatedLine };
                tokens.Add(token);
                ++iIndex;
            }

            if (error)
            {
                token.tokenKind = LexerToken.Kind.PreprocessorDirectiveExpected;
            }
        }
    }

    private new LexerToken ScanIdentifierOrKeyword(string line, ref int startAt)
    {
        var token = Lexer_Base.ScanIdentifierOrKeyword(line, ref startAt);
        if (token != null && token.tokenKind == LexerToken.Kind.Keyword && !IsKeyword(token.text) && !IsBuiltInType(token.text))
            token.tokenKind = LexerToken.Kind.Identifier;
        return token;
    }

}
