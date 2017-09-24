using System;
using System.Collections.Generic;
public class Scope_MemberInitializer : Scope_Base
{
    public Scope_MemberInitializer(SyntaxTreeNode_Rule node) : base(node) { }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;
        if (numTypeArgs == 0 && !asTypeOnly)
        {
            SyntaxTreeNode_Base target = null;

            if (leaf.m_iChildIndex == 0 && leaf.Parent != null && leaf.Parent.Parent == parseTreeNode)
            {
                var node = parseTreeNode
                    .Parent 
                    .Parent 
                    .Parent;
                if (node.RuleName == "objectCreationExpression")
                {
                    target = node.Parent.NodeAt(1); 
                }
                else 
                {
                    target = node.LeafAt(0); 
                }

                if (target != null)
                {
                    var targetSymbol = target.ResolvedSymbol;
                    if (targetSymbol != null)
                        targetSymbol = targetSymbol.TypeOf();
                    else
                        targetSymbol = SymbolDefinition.ResolveNode(target, parentScope);

                    if (targetSymbol != null)
                        targetSymbol.ResolveMember(leaf, parentScope, 0, false);
                    return;
                }
            }
        }

        base.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        var baseNode = completionNode;

        if (baseNode.Parent != null && (baseNode.Parent == parseTreeNode || baseNode.m_iChildIndex == 0 && baseNode.Parent.Parent == parseTreeNode))
        {
            SymbolDefinition target = null;
            SyntaxTreeNode_Base targetNode = null;

            var node = parseTreeNode 
                .Parent 
                .Parent 
                .Parent;
            if (node.RuleName == "objectCreationExpression")
            {
                targetNode = node.Parent;
                target = SymbolDefinition.ResolveNode(targetNode); 
                var targetAsType = target as TypeDefinitionBase;
                if (targetAsType != null)
                    target = targetAsType.GetThisInstance();
            }
            else 
            {
                targetNode = node.Parent.LeafAt(0);
                target = SymbolDefinition.ResolveNode(node.Parent.LeafAt(0)); 
            }

            if (target != null)
            {
                HashSet<SymbolDefinition> completions = new HashSet<SymbolDefinition>();
                SymbolResolver.GetCompletions(IdentifierCompletionsType.Member, targetNode, completions, completionAssetPath);
                foreach (var symbol in completions)
                    data.Add(symbol.name, symbol);
            }
        }
        else
        {
            base.GetCompletionData(data, fromInstance, assembly);
        }
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        return parentScope.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        parentScope.RemoveDeclaration(symbol);
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        throw new InvalidOperationException("Calling FindName on MemberInitializerScope is not allowed!");
    }
}

