using System.Collections.Generic;

public abstract class SD_Invokeable : SymbolDefinition
{
    public abstract TypeDefinitionBase ReturnType();

    protected SymbolReference returnType;
    public List<SD_Instance_Parameter> parameters;
    public List<SD_Type_Parameter> typeParameters;

    public SymbolDefinition AddTypeParameter(SymbolDeclaration symbol)
    {
        var symbolName = symbol.Name;
        if (typeParameters == null)
            typeParameters = new List<SD_Type_Parameter>();
        var definition = typeParameters.Find(x => x.name == symbolName);
        if (definition == null)
        {
            definition = (SD_Type_Parameter)Create(symbol);
            definition.parentSymbol = this;
            typeParameters.Add(definition);
        }

        symbol.definition = definition;

        var nameNode = symbol.NameNode();
        if (nameNode != null)
        {
            var leaf = nameNode as SyntaxTreeNode_Leaf;
            if (leaf != null)
                leaf.SetDeclaredSymbol(definition);
            else
            {
                var lastLeaf = ((SyntaxTreeNode_Rule)nameNode).GetLastLeaf();
                if (lastLeaf != null)
                {
                    if (lastLeaf.Parent.RuleName == "typeParameterList")
                        lastLeaf = lastLeaf.Parent.Parent.LeafAt(0);
                    lastLeaf.SetDeclaredSymbol(definition);
                }
            }
        }

        return definition;
    }

    public bool CanCallWith(Modifiers[] modifiers, bool asExtensionMethod)
    {
        var numArguments = modifiers.Length;

        var minArgs = asExtensionMethod ? 1 : 0;
        var maxArgs = minArgs;
        if (parameters != null)
        {
            for (var i = 0; i < parameters.Count; ++i)
            {
                var param = parameters[i];

                if (i < numArguments)
                {
                    var passedWithOut = modifiers[i] == Modifiers.Out;
                    var passedWithRef = modifiers[i] == Modifiers.Ref;
                    if (param.IsOut != passedWithOut || param.IsRef != passedWithRef)
                        return false;
                }

                if (!asExtensionMethod || !param.IsThisParameter)
                {
                    if (param.IsParametersArray)
                        maxArgs = 100000;
                    else if (!param.IsOptional)
                        ++minArgs;
                    ++maxArgs;
                }
            }
        }
        return !(numArguments < minArgs || numArguments > maxArgs);
    }

    public override SymbolDefinition TypeOf()
    {
        return ReturnType();
    }

    public override List<SD_Instance_Parameter> GetParameters()
    {
        return parameters ?? _emptyParameterList;
    }

    public override List<SD_Type_Parameter> GetTypeParameters()
    {
        return typeParameters;
    }

    public SymbolDefinition AddParameter(SymbolDeclaration symbol)
    {
        var symbolName = symbol.Name;
        var parameter = (SD_Instance_Parameter)Create(symbol);
        parameter.type = new SymbolReference(symbol.parseTreeNode.FindChildByName("type"));
        parameter.parentSymbol = this;
        var lastNode = symbol.parseTreeNode.NodeAt(-1);
        if (lastNode != null && lastNode.RuleName == "defaultArgument")
        {
            var defaultValueNode = lastNode.NodeAt(1);
            if (defaultValueNode != null)
                parameter.defaultValue = defaultValueNode.Print();
        }
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
        else if (symbol.kind == SymbolKind.TypeParameter)
        {
            SymbolDefinition definition = AddTypeParameter(symbol);
            return definition;
        }

        return base.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.Parameter && parameters != null)
            parameters.Remove((SD_Instance_Parameter)symbol.definition);
        else if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
            typeParameters.Remove((SD_Type_Parameter)symbol.definition);
        else
            base.RemoveDeclaration(symbol);
    }

    public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        if (!asTypeOnly && numTypeParameters == 0 && parameters != null)
        {
            var definition = parameters.Find(x => x.name == memberName);
            if (definition != null)
                return definition;
        }
        else
        {
            if (typeParameters != null)
            {
                var definition = typeParameters.Find(x => x.name == memberName);
                if (definition != null)
                    return definition;
            }
        }
        return base.FindName(memberName, numTypeParameters, asTypeOnly);
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (asTypeOnly)
            return;

        if (numTypeArgs == 0)
        {
            var leafText = DecodeId(leaf.token.text);

            if (parameters != null)
            {
                for (var i = parameters.Count; i-- > 0; )
                {
                    if (parameters[i].name == leafText)
                    {
                        leaf.ResolvedSymbol = parameters[i];
                        return;
                    }
                }
            }

            if (typeParameters != null)
            {
                for (var i = typeParameters.Count; i-- > 0; )
                {
                    if (typeParameters[i].name == leafText)
                    {
                        leaf.ResolvedSymbol = typeParameters[i];
                        return;
                    }
                }
            }
        }

        base.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
    }
}

