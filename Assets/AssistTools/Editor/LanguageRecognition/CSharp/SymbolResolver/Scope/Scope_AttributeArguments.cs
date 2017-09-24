using System.Collections.Generic;
using System.Reflection;
public class Scope_AttributeArguments : Scope_Local
{
    public Scope_AttributeArguments(SyntaxTreeNode_Rule node) : base(node) { }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        var attributeTypeLeaf = parseTreeNode.Parent.Parent.NodeAt(0).GetLastLeaf();
        if (attributeTypeLeaf != null)
        {
            var attributeType = attributeTypeLeaf.ResolvedSymbol as TypeDefinitionBase;
            if (attributeType != null)
            {
                var tempData = new Dictionary<string, SymbolDefinition>();
                attributeType.GetMembersCompletionData(tempData, BindingFlags.Instance, AccessLevelMask.Public | AccessLevelMask.Internal, assembly);
                foreach (var kv in tempData)
                {
                    var symbolKind = kv.Value.kind;
                    if (symbolKind == SymbolKind.Field || symbolKind == SymbolKind.Property)
                        if (!data.ContainsKey(kv.Key))
                            data[kv.Key] = kv.Value;
                }
            }
        }
        base.GetCompletionData(data, fromInstance, assembly);
    }
}

