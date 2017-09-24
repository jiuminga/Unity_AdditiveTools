public class Scope_TypeBase : Scope_Base
{
    public TypeDefinitionBase definition;

    public Scope_TypeBase(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        return null;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        return parentScope.FindName(symbolName, numTypeParameters);
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        if (parentScope != null)
            parentScope.Resolve(leaf, numTypeArgs, asTypeOnly);
    }
}

