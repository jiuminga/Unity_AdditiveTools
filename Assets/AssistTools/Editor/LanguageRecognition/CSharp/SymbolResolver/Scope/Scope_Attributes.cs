using System;

using Debug = UnityEngine.Debug;
public class Scope_Attributes : Scope_Base
{
    public Scope_Attributes(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        Debug.LogException(new InvalidOperationException());
        return null;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        Debug.LogException(new InvalidOperationException());
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        var result = parentScope.FindName(symbolName, numTypeParameters);
        return result;
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;
        base.Resolve(leaf, numTypeArgs, asTypeOnly);

        if (leaf.ResolvedSymbol == null || leaf.ResolvedSymbol == SymbolDefinition.unknownSymbol)
        {
            if (leaf.Parent.RuleName == "typeOrGeneric" && leaf.Parent.Parent.Parent.Parent.RuleName == "attribute" &&
                leaf.Parent.m_iChildIndex == leaf.Parent.Parent.NumValidNodes - 1)
            {
                var old = leaf.token.text;
                leaf.token.text += "Attribute";
                leaf.ResolvedSymbol = null;
                base.Resolve(leaf, numTypeArgs, true);
                leaf.token.text = old;
            }
        }
    }
}

