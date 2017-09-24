using System.Collections.Generic;
using System.Linq;
public class Scope_Local : Scope_Base
{
    protected List<SymbolDefinition> localSymbols;

    public Scope_Local(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        symbol.scope = this;
        if (localSymbols == null)
            localSymbols = new List<SymbolDefinition>();

        var definition = SymbolDefinition.Create(symbol);
        localSymbols.Add(definition);

        return definition;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (localSymbols != null)
        {
            localSymbols.RemoveAll((SymbolDefinition x) =>
            {
                if (x.declarations == null)
                    return false;
                if (!x.declarations.Remove(symbol))
                    return false;
                return x.declarations.Count == 0;
            });
        }
        symbol.definition = null;
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;

        if (!asTypeOnly && localSymbols != null)
        {
            var id = SymbolDefinition.DecodeId(leaf.token.text);
            for (var i = localSymbols.Count; i-- > 0;)
            {
                if (localSymbols[i].name == id)
                {
                    leaf.ResolvedSymbol = localSymbols[i];
                    return;
                }
            }
        }

        base.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        symbolName = SymbolDefinition.DecodeId(symbolName);

        if (numTypeParameters == 0 && localSymbols != null)
        {
            for (var i = localSymbols.Count; i-- > 0;)
                if (localSymbols[i].name == symbolName)
                    return localSymbols[i];
        }
        return null;
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (localSymbols != null)
        {
            foreach (var ls in localSymbols)
            {
                SymbolDeclaration declaration = ls.declarations.FirstOrDefault();
                SyntaxTreeNode_Rule declarationNode = declaration != null ? declaration.parseTreeNode : null;
                if (declarationNode == null)
                    continue;
                var firstLeaf = declarationNode.GetFirstLeaf();
                if (firstLeaf != null &&
                    (firstLeaf.Line > completionAtLine ||
                    firstLeaf.Line == completionAtLine && firstLeaf.TokenIndex >= completionAtTokenIndex))
                    continue;
                if (!data.ContainsKey(ls.name))
                    data.Add(ls.name, ls);
            }
        }
        base.GetCompletionData(data, fromInstance, assembly);
    }
}

