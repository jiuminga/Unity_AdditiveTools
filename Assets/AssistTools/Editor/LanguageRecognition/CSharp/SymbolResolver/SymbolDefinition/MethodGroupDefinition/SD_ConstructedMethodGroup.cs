using System.Collections.Generic;
using System.Linq;

using Debug = UnityEngine.Debug;

public class SD_ConstructedMethodGroup : SD_MethodGroup
{
    public readonly SD_MethodGroup genericMethodGroupDefinition;
    public readonly SymbolReference[] typeArguments;

    public override SymbolDefinition GetGenericSymbol()
    {
        return genericMethodGroupDefinition;
    }

    public SD_ConstructedMethodGroup(SD_MethodGroup definition, SymbolReference[] arguments)
    {
        name = definition.name;
        kind = definition.kind;
        parentSymbol = definition.parentSymbol;
        genericMethodGroupDefinition = definition;
        modifiers = definition.modifiers;
        typeArguments = arguments;
    }

    public override MethodDefinition ResolveMethodOverloads(List<TypeDefinitionBase> argumentTypes, Modifiers[] modifiers, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        var genericMethods = genericMethodGroupDefinition.methods;
        methods.RemoveWhere(m => !genericMethods.Contains(m.GetGenericSymbol() as MethodDefinition));
        foreach (var m in genericMethods)
        {
            if (m.NumTypeParameters == typeArguments.Length && methods.All(method => method.GetGenericSymbol() != m))
            {
                var constructedMethod = m.ConstructMethod(typeArguments);
                if (constructedMethod != null)
                    methods.Add(constructedMethod);
            }
        }
        return base.ResolveMethodOverloads(argumentTypes, modifiers, scope, invokedLeaf);
    }

    public override void AddMethod(MethodDefinition method)
    {
        Debug.LogError("AddMethod on ConstructedMethodGroupDefinition: " + method);
    }
}
