
public class SD_Instance_Parameter : SD_Instance
{
    public bool IsThisParameter { get { return modifiers == Modifiers.This; } }

    public bool IsRef { get { return modifiers == Modifiers.Ref; } }
    public bool IsOut { get { return modifiers == Modifiers.Out; } }
    public bool IsParametersArray { get { return modifiers == Modifiers.Params; } }

    public bool IsOptional { get { return defaultValue != null || IsParametersArray; } }
    public string defaultValue;
}

