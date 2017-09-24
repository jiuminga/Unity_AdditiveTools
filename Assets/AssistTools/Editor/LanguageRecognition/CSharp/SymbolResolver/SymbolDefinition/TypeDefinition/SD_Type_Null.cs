
public class SD_Type_Null : TypeDefinitionBase
{
    public override bool CanConvertTo(TypeDefinitionBase otherType)
    {
        return otherType.kind == SymbolKind.Class || otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.Delegate || otherType.kind == SymbolKind.TypeParameter;
    }

    public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType is SD_Type_Parameter)
            return this;

        if (otherType.kind == SymbolKind.Class || otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.Delegate)
            return otherType;

        return null;
    }
}
