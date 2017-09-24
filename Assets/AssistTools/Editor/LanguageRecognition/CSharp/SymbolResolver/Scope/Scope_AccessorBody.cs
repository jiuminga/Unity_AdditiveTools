using System.Collections.Generic;
public class Scope_AccessorBody : Scope_Body
{
    private ValueParameter _value;
    private ValueParameter Value
    {
        get
        {
            if (_value == null || !_value.IsValid())
            {
                definition.parentSymbol.TypeOf();
                _value = new ValueParameter
                {
                    name = "value",
                    kind = SymbolKind.Parameter,
                    parentSymbol = definition,
                    type = ((SD_Instance)definition.parentSymbol).type,
                };
            }
            return _value;
        }
    }

    public Scope_AccessorBody(SyntaxTreeNode_Rule node) : base(node) { }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        if (numTypeParameters == 0 && symbolName == "value" && definition.name != "get")
        {
            return Value;
        }

        return base.FindName(symbolName, numTypeParameters);
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        if (!asTypeOnly && numTypeArgs == 0 && leaf.token.text == "value" && definition.name != "get")
        {
            leaf.ResolvedSymbol = Value;
            return;
        }

        base.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (definition.name != "get")
            data["value"] = Value;
        definition.parentSymbol.GetCompletionData(data, fromInstance, assembly);
        base.GetCompletionData(data, fromInstance, assembly);
    }
}

