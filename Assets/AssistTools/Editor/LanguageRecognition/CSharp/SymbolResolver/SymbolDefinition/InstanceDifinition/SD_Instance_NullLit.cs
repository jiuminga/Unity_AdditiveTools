using System.Collections.Generic;
using System.Reflection;

public class SD_Instance_NullLit : SD_Instance
{
    public readonly SD_Type_Null nullTypeDefinition = new SD_Type_Null();

    public override SymbolDefinition TypeOf()
    {
        return nullTypeDefinition;
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
    }
}

