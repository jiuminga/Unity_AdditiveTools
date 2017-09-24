using System;
using System.Collections.Generic;

public class SD_Type_Delegate : SD_Type
{
    public SymbolReference returnType;
    public List<SD_Instance_Parameter> parameters;

    public override TypeDefinitionBase BaseType()
    {
        if (baseType == null)
            baseType = ReflectedTypeReference.ForType(typeof(MulticastDelegate));
        return baseType.Definition as TypeDefinitionBase;
    }

    public override List<SymbolReference> Interfaces()
    {
        if (interfaces == null)
            interfaces = BaseType().Interfaces();
        return interfaces;
    }

    public override SymbolDefinition TypeOf()
    {
        return returnType != null && returnType.Definition.IsValid() ? returnType.Definition : unknownType;
    }

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

            var nameNode = symbol.NameNode();
            if (nameNode != null)
                nameNode.SetDeclaredSymbol(parameter);
        }
        return parameter;
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.Parameter)
        {
            SymbolDefinition definition = AddParameter(symbol);
            //	if (!members.TryGetValue(symbolName, out definition) || definition is ReflectedMember || definition is ReflectedType)
            //		definition = AddMember(symbol);

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

    public override List<SD_Instance_Parameter> GetParameters()
    {
        return parameters ?? _emptyParameterList;
    }

    private string delegateInfoText;
    public override string GetDelegateInfoText()
    {
        if (delegateInfoText == null)
        {
            delegateInfoText = returnType.Definition.GetName() + " " + GetName() + (parameters != null && parameters.Count == 1 ? "( " : "(");
            delegateInfoText += PrintParameters(parameters) + (parameters != null && parameters.Count == 1 ? " )" : ")");
        }
        return delegateInfoText;
    }
}
