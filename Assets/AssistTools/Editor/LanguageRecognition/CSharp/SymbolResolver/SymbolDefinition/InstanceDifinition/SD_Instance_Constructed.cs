using System.Collections.Generic;
using System.Reflection;

public class SD_Instance_Constructed : SD_Instance
{
    public readonly SD_Instance genericSymbol;

    public SD_Instance_Constructed(SD_Instance genericSymbolDefinition)
    {
        genericSymbol = genericSymbolDefinition;
        kind = genericSymbol.kind;
        modifiers = genericSymbol.modifiers;
        accessLevel = genericSymbol.accessLevel;
        name = genericSymbol.name;
    }

    public override SymbolDefinition TypeOf()
    {
        var result = genericSymbol.TypeOf() as TypeDefinitionBase;

        var ctx = parentSymbol as SD_Type_Constructed;
        if (ctx != null && result != null)
            result = result.SubstituteTypeParameters(ctx);

        return result;
    }

    public override SymbolDefinition GetGenericSymbol()
    {
        return genericSymbol;
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        var symbolType = TypeOf() as TypeDefinitionBase;
        if (symbolType != null)
            symbolType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        var symbolType = TypeOf();
        if (symbolType != null)
            symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
    }
}

