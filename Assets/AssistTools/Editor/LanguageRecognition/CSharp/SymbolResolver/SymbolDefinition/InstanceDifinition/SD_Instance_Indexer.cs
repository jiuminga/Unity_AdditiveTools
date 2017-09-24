using System.Collections.Generic;

public class SD_Instance_Indexer : SD_Instance
{
    public List<SD_Instance_Parameter> parameters;

    public SymbolDefinition AddParameter(SymbolDeclaration symbol)
    {
        var symbolName = symbol.Name;
        var parameter = (SD_Instance_Parameter)Create(symbol);
        parameter.type = new SymbolReference(symbol.parseTreeNode.FindChildByName("type"));
        parameter.parentSymbol = this;
        if (!string.IsNullOrEmpty(symbolName))
        {
            if (parameters == null)
                parameters = new List<SD_Instance_Parameter>();
            parameters.Add(parameter);
        }
        return parameter;
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.Parameter)
        {
            SymbolDefinition definition = AddParameter(symbol);
            symbol.definition = definition;
            return definition;
        }

        return base.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.Parameter && parameters != null)
            parameters.Remove((SD_Instance_Parameter)symbol.definition);
        else
            base.RemoveDeclaration(symbol);
    }

    public override List<SD_Instance_Parameter> GetParameters()
    {
        return parameters ?? _emptyParameterList;
    }

    public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        if (!asTypeOnly && parameters != null)
        {
            var definition = parameters.Find(x => x.name == memberName);
            if (definition != null)
                return definition;
        }
        return base.FindName(memberName, numTypeParameters, asTypeOnly);
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (!asTypeOnly && parameters != null)
        {
            var leafText = DecodeId(leaf.token.text);
            var definition = parameters.Find(x => x.name == leafText);
            if (definition != null)
            {
                leaf.ResolvedSymbol = definition;
                return;
            }
        }
        base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (parameters != null)
        {
            for (var i = parameters.Count; i-- > 0; )
            {
                var p = parameters[i];
                if (!data.ContainsKey(p.name))
                    data.Add(p.name, p);
            }
        }
    }
}

