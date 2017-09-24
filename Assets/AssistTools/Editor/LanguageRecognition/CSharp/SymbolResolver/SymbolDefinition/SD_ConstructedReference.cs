using System.Collections.Generic;
using System.Reflection;

public class SD_ConstructedReference : SymbolDefinition
{
    public readonly SymbolDefinition referencedSymbol;

    public SD_ConstructedReference(SymbolDefinition referencedSymbolDefinition)
    {
        referencedSymbol = referencedSymbolDefinition;
        kind = referencedSymbol.kind;
        modifiers = referencedSymbol.modifiers;
        accessLevel = referencedSymbol.accessLevel;
        name = referencedSymbol.name;
    }

    public override TypeDefinitionBase TypeOfTypeParameter(SD_Type_Parameter tp)
    {
        var fromReferencedSymbol = referencedSymbol.TypeOfTypeParameter(tp);
        var asTypeParameter = fromReferencedSymbol as SD_Type_Parameter;
        if (asTypeParameter != null)
            return base.TypeOfTypeParameter(tp);
        else
            return fromReferencedSymbol;
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        return base.SubstituteTypeParameters(context);
    }

    public override SymbolDefinition TypeOf()
    {
        var result = referencedSymbol.TypeOf() as TypeDefinitionBase;

        var ctx = parentSymbol as SD_Type_Constructed;
        if (ctx != null && result != null)
            result = result.SubstituteTypeParameters(ctx);

        return result;
    }

    public override SymbolDefinition GetGenericSymbol()
    {
        return referencedSymbol.GetGenericSymbol();
    }

    public override List<SD_Instance_Parameter> GetParameters()
    {
        return referencedSymbol.GetParameters();
    }

    public override List<SD_Type_Parameter> GetTypeParameters()
    {
        return referencedSymbol.GetTypeParameters();
    }

    public override void ResolveMember(SyntaxTreeNode_Leaf leaf, Scope_Base context, int numTypeArgs, bool asTypeOnly)
    {
        if (asTypeOnly)
            return;

        var symbolType = TypeOf() as TypeDefinitionBase;
        if (symbolType != null)
            symbolType.ResolveMember(leaf, context, numTypeArgs, asTypeOnly);
    }

    public override SymbolDefinition ResolveMethodOverloads(SyntaxTreeNode_Rule argumentListNode, SymbolReference[] typeArgs, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        if (kind != SymbolKind.MethodGroup)
            return null;
        var genericMethod = ((SD_MethodGroup)referencedSymbol).ResolveMethodOverloads(argumentListNode, typeArgs, scope, invokedLeaf);
        if (genericMethod == null || genericMethod.kind != SymbolKind.Method)
            return null;
        return ((SD_Type_Constructed)parentSymbol).GetConstructedMember(genericMethod);
    }

    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        var symbolType = TypeOf();
        if (symbolType != null)
            symbolType.GetMembersCompletionData(data, BindingFlags.Instance, mask, assembly);
    }
}

