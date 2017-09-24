using System.Text;

public class SD_Method_Constructed : MethodDefinition
{
    public readonly MethodDefinition genericMethodDefinition;
    public readonly SymbolReference[] typeArguments;

    public override bool IsExtensionMethod
    {
        get { return genericMethodDefinition.IsExtensionMethod; }
    }

    public override SymbolDefinition GetGenericSymbol()
    {
        return genericMethodDefinition;
    }

    public SD_Method_Constructed(MethodDefinition definition, SymbolReference[] arguments)
    {
        name = definition.name;
        kind = definition.kind;
        parentSymbol = definition.parentSymbol;
        genericMethodDefinition = definition;
        parameters = genericMethodDefinition.parameters;
        modifiers = genericMethodDefinition.modifiers;

        if (definition.typeParameters != null && arguments != null)
        {
            typeParameters = definition.typeParameters;
            typeArguments = new SymbolReference[typeParameters.Count];
            for (var i = 0; i < typeArguments.Length; ++i)
                typeArguments[i] = i < arguments.Length ? arguments[i] : new SymbolReference(unknownType);
        }
    }

    public override TypeDefinitionBase TypeOfTypeParameter(SD_Type_Parameter tp)
    {
        if (typeParameters != null)
        {
            var index = typeParameters.IndexOf(tp);
            if (index >= 0)
                return typeArguments[index].Definition as TypeDefinitionBase ?? tp;
        }
        return base.TypeOfTypeParameter(tp);
    }

    public override TypeDefinitionBase ReturnType()
    {
        var result = genericMethodDefinition.ReturnType();
        result = result.SubstituteTypeParameters(this);
        return result;
    }

    public override string GetName()
    {
        var typeParameters = GetTypeParameters();
        if (typeParameters == null || typeParameters.Count == 0)
            return name;

        var sb = new StringBuilder();
        sb.Append(name);
        sb.Append('<');
        sb.Append(TypeOfTypeParameter(typeParameters[0]).GetName());
        for (var i = 1; i < typeParameters.Count; ++i)
        {
            sb.Append(", ");
            sb.Append(TypeOfTypeParameter(typeParameters[i]).GetName());
        }
        sb.Append('>');
        return sb.ToString();
    }
}

