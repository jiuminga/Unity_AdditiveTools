using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

public class MethodDefinition: SD_Invokeable
{
    protected bool isExtensionMethod;
    public override bool IsExtensionMethod
    {
        get { return isExtensionMethod; }
    }

    public MethodDefinition()
    {
        kind = SymbolKind.Method;
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        var result = base.AddDeclaration(symbol);

        if (IsStatic && result.kind == SymbolKind.Parameter && result.modifiers == Modifiers.This &&
            symbol.parseTreeNode != null && symbol.parseTreeNode.Parent != null && symbol.parseTreeNode.Parent.m_iChildIndex == 0)
        {
            var parentType = (parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol) as TypeDefinitionBase;
            if (parentType.kind == SymbolKind.Class && parentType.IsStatic && parentType.NumTypeParameters == 0)
            {
                var namespaceDefinition = parentType.parentSymbol;
                if (namespaceDefinition is SD_NameSpace)
                {
                    isExtensionMethod = true;
                    ++parentType.numExtensionMethods;
                }
            }
        }

        return result;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (IsExtensionMethod && symbol.kind == SymbolKind.Parameter && symbol.definition.modifiers == Modifiers.This &&
            (symbol.parseTreeNode == null || symbol.parseTreeNode.Parent == null || symbol.parseTreeNode.Parent.m_iChildIndex == 0))
        {
            isExtensionMethod = false;

            var parentType = (parentSymbol.kind == SymbolKind.MethodGroup ? parentSymbol.parentSymbol : parentSymbol) as TypeDefinitionBase;

            var namespaceDefinition = parentType.parentSymbol;
            if (namespaceDefinition is SD_NameSpace)
                --parentType.numExtensionMethods;
        }
        base.RemoveDeclaration(symbol);
    }

    public override TypeDefinitionBase ReturnType()
    {
        if (returnType == null)
        {
            if (kind == SymbolKind.Constructor)
                return parentSymbol as TypeDefinitionBase ?? unknownType;

            if (declarations != null)
            {
                SyntaxTreeNode_Base refNode = null;
                switch (declarations[0].parseTreeNode.RuleName)
                {
                    case "methodDeclaration":
                    case "interfaceMethodDeclaration":
                        refNode = declarations[0].parseTreeNode.FindPreviousNode();
                        break;
                    default:
                        refNode = declarations[0].parseTreeNode.Parent.Parent.ChildAt(declarations[0].parseTreeNode.Parent.m_iChildIndex - 1);
                        break;
                }
                if (refNode == null)
                    Debug.LogError("Could not find method return type from node: " + declarations[0].parseTreeNode);
                returnType = refNode != null ? new SymbolReference(refNode) : null;
            }
        }

        return returnType == null ? unknownType : returnType.Definition as TypeDefinitionBase ?? unknownType;
    }

    //public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    //{
    //	if (typeParameters == null)
    //		return base.SubstituteTypeParameters(context);

    //	var constructType = false;
    //	var typeArguments = new SymbolReference[typeParameters.Count];
    //	for (var i = 0; i < typeArguments.Length; ++i)
    //	{
    //		typeArguments[i] = new SymbolReference(typeParameters[i]);
    //		var original = typeParameters[i];
    //		if (original == null)
    //			continue;
    //		var substitute = original.SubstituteTypeParameters(context);
    //		if (substitute != original)
    //		{
    //			typeArguments[i] = new SymbolReference(substitute);
    //			constructType = true;
    //		}
    //	}
    //	if (!constructType)
    //		return this;
    //	return ConstructType(typeArguments);
    //}


    public override void GetMembersCompletionData(Dictionary<string, SymbolDefinition> data, BindingFlags flags, AccessLevelMask mask, SD_Assembly assembly)
    {
        foreach (var parameter in GetParameters())
        {
            var parameterName = parameter.GetName();
            if (!data.ContainsKey(parameterName))
                data.Add(parameterName, parameter);
        }
        if ((flags & (BindingFlags.Instance | BindingFlags.Static)) != BindingFlags.Instance)
        {
            if (typeParameters != null)
                foreach (var parameter in typeParameters)
                {
                    var parameterName = parameter.name;
                    if (!data.ContainsKey(parameterName))
                        data.Add(parameterName, parameter);
                }
        }
        //	ReturnType().GetMembersCompletionData(data, flags, mask, assembly);
    }

    private Dictionary<int, SD_Method_Constructed> constructedMethods;
    public SD_Method_Constructed ConstructMethod(SymbolReference[] typeArgs)
    {
        var numTypeParams = typeParameters != null ? typeParameters.Count : 0;
        var numTypeArgs = typeArgs != null ? typeArgs.Length : 0;

        var hash = 0;
        if (typeArgs != null)
        {
            unchecked // ignore overflow
            {
                hash = (int)2166136261;
                for (var i = 0; i < numTypeParams; ++i)
                    hash = hash * 16777619 ^ (i < numTypeArgs ? typeArgs[i].Definition : unknownType).GetHashCode();
            }
        }

        if (constructedMethods == null)
            constructedMethods = new Dictionary<int, SD_Method_Constructed>();

        SD_Method_Constructed result;
        if (constructedMethods.TryGetValue(hash, out result))
        {
            if (result.IsValid() && result.typeArguments != null)
            {
                var validCachedMethod = true;
                var resultTypeArgs = result.typeArguments;
                for (var i = 0; i < numTypeParams; ++i)
                {
                    var definition = resultTypeArgs[i].Definition;
                    var typeArg = i < numTypeArgs ? typeArgs[i].Definition : unknownType;
                    if (definition == null || !definition.IsValid() || definition != typeArg)
                    {
                        validCachedMethod = false;
                        break;
                    }
                }
                if (validCachedMethod)
                    return result;
            }
        }

        result = new SD_Method_Constructed(this, typeArgs);
        constructedMethods[hash] = result;
        return result;
    }

    public bool IsOverride
    {
        get { return (modifiers & Modifiers.Override) != 0; }
    }

    public bool IsVirtual
    {
        get { return (modifiers & Modifiers.Virtual) != 0; }
    }
}

