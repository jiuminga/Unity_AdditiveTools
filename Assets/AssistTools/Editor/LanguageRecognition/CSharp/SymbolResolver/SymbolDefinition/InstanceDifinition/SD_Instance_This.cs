
public class SD_Instance_This : SD_Instance
{
    public SD_Instance_This(SymbolReference type)
    {
        this.type = type;
        kind = SymbolKind.Instance;
    }

    public override string GetTooltipText()
    {
        return type.Definition.GetTooltipText();
    }
}

