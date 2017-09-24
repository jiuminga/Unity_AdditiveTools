using System;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;
public class Scope_SymbolDeclaration : Scope_Base
{
    public SymbolDeclaration declaration;

    public Scope_SymbolDeclaration(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.scope == null)
            symbol.scope = this;
        if (declaration == null)
        {
            Debug.LogWarning("Missing declaration in SymbolDeclarationScope! Can't add " + symbol + "\nfor node: " + parseTreeNode);
            return null;
        }
        return declaration.definition.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if ((symbol.kind == SymbolKind.Method) && declaration == symbol)
        {
            declaration = null;
            parentScope.RemoveDeclaration(symbol);
        }
        else if (declaration != null && declaration.definition != null)
        {
            declaration.definition.RemoveDeclaration(symbol);
        }
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        throw new NotImplementedException();
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        if (declaration != null && declaration.definition != null)
        {
            declaration.definition.ResolveMember(leaf, this, numTypeArgs, asTypeOnly);

            if (numTypeArgs == 0 && leaf.ResolvedSymbol == null)
            {
                var typeParams = declaration.definition.GetTypeParameters();
                if (typeParams != null)
                {
                    var id = SymbolDefinition.DecodeId(leaf.token.text);
                    for (int i = typeParams.Count; i-- > 0;)
                    {
                        if (typeParams[i].GetName() == id)
                        {
                            leaf.ResolvedSymbol = typeParams[i];
                            break;
                        }
                    }
                }
            }
        }

        if (leaf.ResolvedSymbol == null)
            base.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public override void ResolveAttribute(SyntaxTreeNode_Leaf leaf)
    {
        if (declaration != null)
            declaration.definition.ResolveAttributeMember(leaf, this);

        if (leaf.ResolvedSymbol == null)
            base.ResolveAttribute(leaf);
    }

    public override SD_Type EnclosingType()
    {
        if (declaration != null)
        {
            switch (declaration.kind)
            {
                case SymbolKind.Class:
                case SymbolKind.Struct:
                case SymbolKind.Interface:
                    return (SD_Type)declaration.definition;
            }
        }
        return parentScope != null ? parentScope.EnclosingType() : null;
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (declaration != null && declaration.definition != null)
        {
            var typeParameters = declaration.definition.GetTypeParameters();
            if (typeParameters != null)
            {
                for (var i = typeParameters.Count; i-- > 0;)
                {
                    var tp = typeParameters[i];
                    if (!data.ContainsKey(tp.name))
                        data.Add(tp.name, tp);
                }
            }
        }
        base.GetCompletionData(data, fromInstance, assembly);
    }
}

