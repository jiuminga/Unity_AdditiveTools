using System.Collections.Generic;

public class SD_Type_Array : SD_Type
{
    public readonly TypeDefinitionBase elementType;
    public readonly int rank;

    private List<SymbolReference> arrayGenericInterfaces;

    public SD_Type_Array(TypeDefinitionBase elementType, int rank)
    {
        kind = SymbolKind.Class;
        this.elementType = elementType;
        this.rank = rank;
        name = elementType.GetName() + RankString();
    }

    public override TypeDefinitionBase BaseType()
    {
        if (arrayGenericInterfaces == null && rank == 1)
            Interfaces();
        return builtInTypes_Array;
    }

    public override List<SymbolReference> Interfaces()
    {
        if (arrayGenericInterfaces == null && rank == 1)
        {
            arrayGenericInterfaces = new List<SymbolReference> {
                ReflectedTypeReference.ForType(typeof(IEnumerable<>)),
                ReflectedTypeReference.ForType(typeof(IList<>)),
                ReflectedTypeReference.ForType(typeof(ICollection<>)),
            };

            var typeArguments = new[] { new SymbolReference(elementType) };
            for (var i = 0; i < arrayGenericInterfaces.Count; ++i)
            {
                var genericInterface = arrayGenericInterfaces[i].Definition as SD_Type;
                genericInterface = genericInterface.ConstructType(typeArguments);
                arrayGenericInterfaces[i] = new SymbolReference(genericInterface);
            }
        }
        interfaces = arrayGenericInterfaces ?? base.Interfaces();
        return interfaces;
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        var constructedElement = elementType.SubstituteTypeParameters(context);
        if (constructedElement != elementType)
            return constructedElement.MakeArrayType(rank);

        return base.SubstituteTypeParameters(context);
    }

    protected override string RankString()
    {
        return '[' + new string(',', rank - 1) + ']';
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters, bool asTypeOnly)
    {
        symbolName = DecodeId(symbolName);

        var result = base.FindName(symbolName, numTypeParameters, asTypeOnly);
        //		if (result == null && BaseType() != null)
        //		{
        //			//	Debug.Log("Symbol lookup '" + symbolName +"' in base " + baseType.definition);
        //			result = BaseType().FindName(symbolName, numTypeParameters, asTypeOnly);
        //		}
        return result;
    }

    public override string GetTooltipText()
    {
        //		if (tooltipText != null)
        //			return tooltipText;

        if (elementType == null)
            return "array of unknown type";

        if (parentSymbol != null && !string.IsNullOrEmpty(parentSymbol.GetName()))
            tooltipText = parentSymbol.GetName() + "." + elementType.GetName() + RankString();
        tooltipText = elementType.GetName() + RankString();

        var xmlDocs = GetXmlDocs();
        if (!string.IsNullOrEmpty(xmlDocs))
        {
            tooltipText += "\n\n" + xmlDocs;
        }

        return tooltipText;
    }

    public override bool CanConvertTo(TypeDefinitionBase otherType)
    {
        var asArrayType = otherType as SD_Type_Array;
        if (asArrayType != null)
        {
            if (rank != asArrayType.rank)
                return false;
            return elementType.CanConvertTo(asArrayType.elementType);
        }

        if (rank == 1 && (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter))
        {
            var genericInterfaces = Interfaces();
            for (var i = 0; i < genericInterfaces.Count; ++i)
            {
                var type = genericInterfaces[i].Definition as TypeDefinitionBase;
                if (type != null && type.CanConvertTo(otherType))
                    return true;
            }
        }

        return base.CanConvertTo(otherType);
    }

    public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType is SD_Type_Parameter)
            return this;

        var asArrayType = otherType as SD_Type_Array;
        if (asArrayType != null)
        {
            if (rank != asArrayType.rank)
                return null;

            var convertedElementType = elementType.ConvertTo(asArrayType.elementType);
            if (convertedElementType == null)
                return null;

            if (convertedElementType == elementType)
                return this;

            return convertedElementType.MakeArrayType(rank);
        }

        if (rank == 1 && otherType.kind == SymbolKind.Interface)
        {
            var genericInterfaces = Interfaces();
            for (var i = 0; i < genericInterfaces.Count; ++i)
            {
                var interfaceType = genericInterfaces[i].Definition as TypeDefinitionBase;
                var constructedInterface = interfaceType.ConvertTo(otherType);
                if (constructedInterface != null)
                    return constructedInterface;
            }
        }

        return base.ConvertTo(otherType);
    }

    internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        var argumentAsArray = argumentType as SD_Type_Array;
        if (argumentAsArray != null && argumentAsArray.rank == rank)
        {
            var boundElementType = elementType.BindTypeArgument(typeArgument, argumentAsArray.elementType);
            if (boundElementType != null)
                return boundElementType;
        }
        return base.BindTypeArgument(typeArgument, argumentType);
    }
}

