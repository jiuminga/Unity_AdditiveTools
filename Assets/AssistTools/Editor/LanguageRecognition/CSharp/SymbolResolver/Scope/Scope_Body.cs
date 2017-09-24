using System.Collections.Generic;
using System.Linq;
public class Scope_Body : Scope_Local
{
    public SymbolDefinition definition;

    public Scope_Body(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (definition == null)
            return null;

        symbol.scope = this;

        switch (symbol.kind)
        {
            case SymbolKind.ConstantField:
            case SymbolKind.LocalConstant:
                if (!(definition is TypeDefinitionBase))
                    return base.AddDeclaration(symbol);
                break;
            case SymbolKind.Variable:
            case SymbolKind.ForEachVariable:
            case SymbolKind.FromClauseVariable:
                return base.AddDeclaration(symbol);
        }

        return definition.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        switch (symbol.kind)
        {
            case SymbolKind.LocalConstant:
            case SymbolKind.Variable:
            case SymbolKind.ForEachVariable:
            case SymbolKind.FromClauseVariable:
                base.RemoveDeclaration(symbol);
                return;
        }

        if (definition != null)
            definition.RemoveDeclaration(symbol);
        base.RemoveDeclaration(symbol);
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        return definition.FindName(symbolName, numTypeParameters, false);
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;

        if (definition != null)
        {
            definition.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);

            if (leaf.ResolvedSymbol != null)
                return;

            if (numTypeArgs == 0 && leaf.ResolvedSymbol == null)
            {
                var typeParams = definition.GetTypeParameters();
                if (typeParams != null)
                {
                    var id = SymbolDefinition.DecodeId(leaf.token.text);
                    for (var i = typeParams.Count; i-- > 0;)
                    {
                        if (typeParams[i].GetName() == id)
                        {
                            leaf.ResolvedSymbol = typeParams[i];
                            return;
                        }
                    }
                }
            }
        }

        base.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public override void ResolveAttribute(SyntaxTreeNode_Leaf leaf)
    {
        leaf.ResolvedSymbol = null;
        if (definition != null)
            definition.ResolveAttributeMember(leaf, this);

        if (leaf.ResolvedSymbol == null)
            base.ResolveAttribute(leaf);
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (definition != null)
            definition.GetCompletionData(data, fromInstance, assembly);

        Scope_Base scope = this;
        while (fromInstance && scope != null)
        {
            var asBodyScope = scope as Scope_Body;
            if (asBodyScope != null)
            {
                var symbol = asBodyScope.definition;
                if (symbol != null && symbol.kind != SymbolKind.LambdaExpression)
                {
                    if (!symbol.IsInstanceMember)
                        fromInstance = false;
                    break;
                }
            }
            scope = scope.parentScope;
        }
        base.GetCompletionData(data, fromInstance, assembly);
    }
}

