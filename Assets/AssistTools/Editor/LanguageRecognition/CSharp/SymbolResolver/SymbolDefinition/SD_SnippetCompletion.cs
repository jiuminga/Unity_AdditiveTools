using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

class SD_SnippetCompletion : SymbolDefinition
{
    protected string displayFormat = "{0}";
    protected string expandedText;

    public SD_SnippetCompletion(string name)
    {
        this.name = name;
        kind = SymbolKind._Snippet;
    }

    public SD_SnippetCompletion(string name, string displayFormat)
        : this(name)
    {
        this.displayFormat = displayFormat;
    }

    public SD_SnippetCompletion(string name, string displayFormat, string expandTo)
        : this(name, displayFormat)
    {
        this.expandedText = expandTo;
    }

    public override string CompletionDisplayString(string styledName)
    {
        return string.Format(displayFormat, styledName);
    }

    public virtual string Expand()
    {
        return expandedText;
    }

    public virtual void OverrideTypedInLength(ref int typedInLength)
    {
        // By default typedInLength remains unmodified
    }
}

interface ISnippetProvider
{
    IEnumerable<SD_SnippetCompletion> EnumSnippets(
        SymbolDefinition context,
        TokenSet expectedTokens,
        LexerToken tokenLeft,
        Scope_Base scope
    );

    string Get(
        string shortcut,
        SymbolDefinition context,
        TokenSet expectedTokens,
        Scope_Base scope
    );
}

class CodeSnippets : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (snippets == null)
            return;

        var path = GetSnippetsPath();
        if (path == null)
            return;

        System.Predicate<string> snippet = x => x.StartsWith(path, System.StringComparison.OrdinalIgnoreCase);

        if (System.Array.Exists(importedAssets, snippet) ||
            System.Array.Exists(deletedAssets, snippet) ||
            System.Array.Exists(movedAssets, snippet) ||
            System.Array.Exists(movedFromAssetPaths, snippet))
        {
            Reload();
        }
    }

    private static List<ISnippetProvider> snippetsProviders;

    private static Dictionary<string, string> _snippets;
    private static Dictionary<string, string> snippets
    {
        get
        {
            if (_snippets == null)
                Reload();
            return _snippets;
        }
        set
        {
            _snippets = value;
        }
    }

    public static string Get(string shortcut, SymbolDefinition context, TokenSet expected)
    {
        string text;
        if (!snippets.TryGetValue(shortcut, out text))
            return null;

        if (!IsValid(ref text, context, expected))
            return null;

        return text;
    }

    public static IEnumerable<SD_SnippetCompletion> EnumSnippets(SymbolDefinition context, TokenSet expected, LexerToken tokenLeft, Scope_Base scope)
    {
        foreach (var snippet in snippets)
        {
            var text = snippet.Value;
            if (IsValid(ref text, context, expected))
                yield return new SD_SnippetCompletion(snippet.Key + "...");
        }

        if (snippetsProviders == null)
        {
            snippetsProviders = new List<ISnippetProvider>();
            var types = typeof(CodeSnippets).Assembly.GetTypes();
            foreach (var type in types)
                if (typeof(ISnippetProvider).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                    try
                    {
                        var instance = System.Activator.CreateInstance(type) as ISnippetProvider;
                        snippetsProviders.Add(instance);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

        }

        foreach (var snippetsProvider in snippetsProviders)
        {
            foreach (var snippet in snippetsProvider.EnumSnippets(context, expected, tokenLeft, scope))
                yield return snippet;
        }
    }

    public static void Substitute(ref string part, SymbolDefinition context)
    {
        var methodDef = context as MethodDefinition;

        int index = 0;
        while ((index = part.IndexOf('$', index)) != -1)
        {
            var end = part.IndexOf('$', index + 1);
            if (end > index)
            {
                var macro = part.Substring(index + 1, end - index - 1);
                part = part.Remove(index, end - index + 1);
                switch (macro)
                {
                    case "MethodName":
                        if (methodDef == null)
                            goto default;
                        part = part.Insert(index, methodDef.name);
                        break;
                    case "ArgumentList":
                        if (methodDef == null)
                            goto default;
                        part = part.Insert(index, string.Join(", ",
                            (
                                from p in methodDef.GetParameters()
                                select (p.IsOut ? "out " : p.IsRef ? "ref " : "") + p.GetName()
                            ).ToArray()));
                        break;
                    default:
                        part = part.Insert(index, macro);
                        break;
                }
            }
        }
    }

    public static bool IsValid(ref string expanded, SymbolDefinition context, TokenSet expected)
    {
        var methodDef = context as MethodDefinition;
        var checkKeyword = false;

        for (var index = 0; (index = expanded.IndexOf('$', index)) != -1; )
        {
            var end = expanded.IndexOf('$', index + 1);
            if (end < index)
                break;

            var macro = expanded.Substring(index + 1, end - index - 1);
            switch (macro)
            {
                case "MethodName":
                case "ArgumentList":
                    if (methodDef == null)
                        return false;
                    break;

                case "ValidIfKeywordExpected":
                    checkKeyword = true;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsStatement":
                    if (expected != null && !expected.Contains(Parser_CSharp.Instance.tokenStatement))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "NotValidAsStatement":
                    if (expected != null && expected.Contains(Parser_CSharp.Instance.tokenStatement))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsClassMember":
                    if (expected != null && !expected.Contains(Parser_CSharp.Instance.tokenClassBody))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsStructMember":
                    if (expected != null && !expected.Contains(Parser_CSharp.Instance.tokenStructBody))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsInterfaceMember":
                    if (expected != null && !expected.Contains(Parser_CSharp.Instance.tokenInterfaceBody))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsNamespaceMember":
                    if (expected != null && !expected.Contains(Parser_CSharp.Instance.tokenNamespaceBody))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidAsTypeDeclaration":
                    if (expected != null && !(
                        expected.Contains(Parser_CSharp.Instance.tokenNamespaceBody) ||
                        expected.Contains(Parser_CSharp.Instance.tokenClassBody) ||
                        expected.Contains(Parser_CSharp.Instance.tokenStructBody)
                    ))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidInMethodBody":
                    if (methodDef == null)
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;

                case "ValidInPropertyBody":
                    if (context == null ||
                        context.kind != SymbolKind.Property &&
                        (context.parentSymbol == null || context.parentSymbol.kind != SymbolKind.Property))
                        return false;
                    expanded = expanded.Remove(index, end - index + 2);
                    continue;
            }
            index = end + 1;
        }

        if (checkKeyword && expected != null)
        {
            var index = 0;
            while (expanded[index] >= 'a' && expanded[index] <= 'z')
                ++index;
            if (index == 0)
            {
                if (expanded.StartsWith("!=", System.StringComparison.Ordinal) || expanded.StartsWith("==", System.StringComparison.Ordinal))
                    index = 2;
                else
                    return false;
            }
            var keyword = expanded.Substring(0, index);
            var tokenId = Parser_CSharp.Instance.TokenToId(keyword);
            if (!expected.Contains(tokenId))
                return false;
        }

        return true;
    }

    private static string GetSnippetsPath()
    {
        //var managerScript = MonoScript.FromScriptableObject(FGTextBufferManager.Instance());
        //if (!managerScript)
        //    return null;
        //var path = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(managerScript)));
        //return path + "/CodeSnippets/";
        return null;
    }

    private static void Reload()
    {
        snippets = new Dictionary<string, string>();

        var path = GetSnippetsPath();
        if (path == null)
            return;

        var all = System.IO.Directory.GetFiles(path, "*.txt");
        foreach (var asset in all)
        {
            var snippet = AssetDatabase.LoadAssetAtPath(asset, typeof(TextAsset)) as TextAsset;
            if (snippet == null)
                continue;
            snippets[snippet.name] = snippet.text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}

