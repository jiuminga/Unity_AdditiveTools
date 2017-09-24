using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SD_MethodGroup : SymbolDefinition
{
    public static readonly MethodDefinition ambiguousMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "ambiguous method overload" };
    public static readonly MethodDefinition unresolvedMethodOverload = new MethodDefinition { kind = SymbolKind.Error, name = "unresolved method overload" };

    public HashSet<MethodDefinition> methods = new HashSet<MethodDefinition>();

    public virtual void AddMethod(MethodDefinition method)
    {
        methods.RemoveWhere((MethodDefinition x) => !x.IsValid());
        if (method.declarations != null)
        {
            var d = method.declarations[0];
            methods.RemoveWhere((MethodDefinition x) => x.declarations != null && x.declarations.Contains(d));
        }
        methods.Add(method);
        method.parentSymbol = this;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        //Debug.Log("removing " + symbol.Name + " - " + (symbol.parseTreeNode.ChildAt(0) ?? symbol.parseTreeNode).Print());
        methods.RemoveWhere((MethodDefinition x) => x.declarations.Contains(symbol));
    }

    public SymbolDefinition ResolveParameterName(SyntaxTreeNode_Leaf leaf)
    {
        foreach (var m in methods)
        {
            var p = m.GetParameters();
            var leafText = DecodeId(leaf.token.text);
            var x = p.Find(pd => pd.name == leafText);
            if (x != null)
                return leaf.ResolvedSymbol = x;
        }
        return unknownSymbol;
    }

    public static int ProcessArgumentListNode(SyntaxTreeNode_Rule argumentListNode, out Modifiers[] modifiers, out List<TypeDefinitionBase> argumentTypes, TypeDefinitionBase extendedType)
    {
        var numArguments = argumentListNode == null ? 0 : (argumentListNode.NumValidNodes + 1) / 2;
        var thisOffest = 0;
        if (extendedType != null)
        {
            thisOffest = 1;
            ++numArguments;
        }

        modifiers = new Modifiers[numArguments];
        argumentTypes = new List<TypeDefinitionBase>();
        var resolvedArguments = new SymbolDefinition[numArguments];

        if (extendedType != null)
        {
            argumentTypes.Add(extendedType);
        }

        for (var i = thisOffest; i < numArguments; ++i)
        {
            var argumentNode = argumentListNode.NodeAt((i - thisOffest) * 2);
            if (argumentNode != null)
            {
                var argumentValueNode = argumentNode.FindChildByName("argumentValue") as SyntaxTreeNode_Rule;
                if (argumentValueNode != null)
                {
                    resolvedArguments[i] = ResolveNode(argumentValueNode);
                    if (resolvedArguments[i] != null)
                        argumentTypes.Add(resolvedArguments[i].TypeOf() as TypeDefinitionBase ?? unknownType);
                    else
                        argumentTypes.Add(unknownType);

                    var modifierLeaf = argumentValueNode.LeafAt(0);
                    if (modifierLeaf != null)
                    {
                        if (modifierLeaf.IsLit("ref"))
                            modifiers[i] = Modifiers.Ref;
                        else if (modifierLeaf.IsLit("out"))
                            modifiers[i] = Modifiers.Out;
                    }

                    continue;
                }
            }

            numArguments = i;
            break;
        }

        return numArguments;
    }

    public override SymbolDefinition ResolveMethodOverloads(SyntaxTreeNode_Rule argumentListNode, SymbolReference[] typeArgs, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        Modifiers[] modifiers;
        List<TypeDefinitionBase> argumentTypes;

        ProcessArgumentListNode(argumentListNode, out modifiers, out argumentTypes, null);

        var resolved = ResolveMethodOverloads(argumentTypes, modifiers, scope, invokedLeaf);
        return resolved;
    }

    public List<MethodDefinition> CollectCandidates(List<TypeDefinitionBase> argumentTypes, Modifiers[] modifiers, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        var accessLevelMask = AccessLevelMask.Public;
        var parentType = parentSymbol as TypeDefinitionBase ?? parentSymbol.parentSymbol as TypeDefinitionBase;
        var contextType = scope.EnclosingType();
        if (contextType != null)
        {
            if (parentType.Assembly != null && parentType.Assembly.InternalsVisibleIn(contextType.Assembly))
                accessLevelMask |= AccessLevelMask.Internal;

            if (contextType == parentType || parentType.IsSameOrParentOf(contextType))
                accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected | AccessLevelMask.Private;
            else if (contextType.DerivesFrom(parentType))
                accessLevelMask |= AccessLevelMask.Public | AccessLevelMask.Protected;
        }

        var candidates = new List<MethodDefinition>();
        foreach (var method in methods)
            if (!method.IsOverride && method.IsAccessible(accessLevelMask) &&
                (argumentTypes == null || method.CanCallWith(modifiers, false)))
                candidates.Add(method);

        var thisAsConstructedMG = this as SD_ConstructedMethodGroup;
        for (var i = candidates.Count; i-- > 0; )
        {
            var candidate = candidates[i];
            if (thisAsConstructedMG == null)
            {
                if (candidate.NumTypeParameters == 0 || argumentTypes == null)
                    continue;

                candidate = InferMethodTypeArguments(candidate, argumentTypes, invokedLeaf);
                if (candidate == null)
                    candidates.RemoveAt(i);
                else
                    candidates[i] = candidate;
            }
            else
            {
                candidate = candidate.ConstructMethod(thisAsConstructedMG.typeArguments);
            }
        }

        if (candidates.Count != 0)
            return candidates;

        var baseType = (TypeDefinitionBase)parentSymbol;
        while ((baseType = baseType.BaseType()) != null)
        {
            var baseSymbol = baseType.FindName(name, 0, false) as SD_MethodGroup;
            if (baseSymbol != null)
                return baseSymbol.CollectCandidates(argumentTypes, modifiers, scope, invokedLeaf);
        }
        return null;
    }

    private static List<int> GenerateRangeList(int to)
    {
        var list = new List<int>();
        for (var i = 0; i < to; ++i)
            list.Add(i);
        return list;
    }

    public static MethodDefinition InferMethodTypeArguments(MethodDefinition method, List<TypeDefinitionBase> argumentTypes, SyntaxTreeNode_Leaf invokedLeaf)
    {
        var numTypeParameters = method.NumTypeParameters;
        List<TypeDefinitionBase> typeArgs = new List<TypeDefinitionBase>();
        foreach (var item in method.typeParameters)
            typeArgs.Add(item);

        var parameters = method.GetParameters();
        var numParameters = Math.Min(parameters.Count, argumentTypes.Count);

        var openTypeArguments = GenerateRangeList(numTypeParameters);

        var stayInLoop = true;
        while (stayInLoop)
        {
            stayInLoop = false;
            for (var i = openTypeArguments.Count; i-- > 0; )
            {
                var typeArgIndex = openTypeArguments[i];
                var typeArgument = typeArgs[typeArgIndex];

                for (var j = numParameters; j-- > 0; )
                {
                    var argumentType = argumentTypes[j];
                    if (argumentType == null)
                        continue;

                    var parameter = parameters[j]; //TODO: Consider expanded params parameter and all arguments
                    var parameterType = parameter.TypeOf() as TypeDefinitionBase;

                    parameterType = parameterType.SubstituteTypeParameters(method);

                    if (parameterType != null && parameterType.IsValid())
                    {
                        var boundType = parameterType.BindTypeArgument(typeArgument, argumentType);
                        if (boundType != null && boundType != typeArgument && boundType.kind != SymbolKind.Error)
                        {
                            typeArgs[typeArgIndex] = boundType;
                            openTypeArguments.RemoveAt(i);
                            stayInLoop = openTypeArguments.Count > 0;

                            if (stayInLoop)
                            {
                                var newTypeArguments = new SymbolReference[typeArgs.Count];
                                for (var k = typeArgs.Count; k-- > 0; )
                                    newTypeArguments[k] = new SymbolReference(typeArgs[k]);
                                method = method.ConstructMethod(newTypeArguments);
                                if (invokedLeaf != null)
                                    invokedLeaf.ResolvedSymbol = method;
                            }

                            //TODO: Should actually use the lower and upper bounds
                            break;
                        }
                    }
                }
            }
        }

        var typeArgRefs = new SymbolReference[numTypeParameters];
        for (var i = 0; i < numTypeParameters; ++i)
            typeArgRefs[i] = new SymbolReference(typeArgs[i] ?? builtInTypes_object);
        method = method.ConstructMethod(typeArgRefs);
        return method;
    }

    public virtual MethodDefinition ResolveMethodOverloads(List<TypeDefinitionBase> argumentTypes, Modifiers[] modifiers, Scope_Base scope, SyntaxTreeNode_Leaf invokedLeaf)
    {
        var candidates = CollectCandidates(argumentTypes, modifiers, scope, invokedLeaf);
        if (candidates == null)
            return unresolvedMethodOverload;

        return ResolveMethodOverloads(argumentTypes.Count, argumentTypes, modifiers, candidates);
    }

    public static MethodDefinition ResolveMethodOverloads(int numArguments, List<TypeDefinitionBase> argumentTypes, Modifiers[] modifiers, List<MethodDefinition> candidates)
    {
        // find best match
        MethodDefinition bestMatch = null;
        var bestExactMatches = -1;
        foreach (var method in candidates)
        {
            var parameters = method.GetParameters();
            var expandParams = true;

        tryNotExpanding:

            var exactMatches = 0;
            SD_Instance_Parameter paramsArray = null;
            for (var i = 0; i < UnityEngine.Mathf.Min(numArguments, parameters.Count); ++i)
            {
                if (argumentTypes[i] == null)
                {
                    exactMatches = -1;
                    break;
                }

                if (expandParams && paramsArray == null && parameters[i].IsParametersArray)
                    paramsArray = parameters[i];

                TypeDefinitionBase parameterType = null;
                if (paramsArray != null)
                {
                    var arrayType = paramsArray.TypeOf() as SD_Type_Array;
                    if (arrayType != null)
                        parameterType = arrayType.elementType;
                }
                else
                {
                    if (i >= parameters.Count)
                    {
                        exactMatches = -1;
                        break;
                    }
                    parameterType = parameters[i].TypeOf() as TypeDefinitionBase;
                }
                parameterType = parameterType == null ? unknownType : parameterType.SubstituteTypeParameters(method);

                if (argumentTypes[i].IsSameType(parameterType))
                {
                    ++exactMatches;
                    continue;
                }
                if (!argumentTypes[i].CanConvertTo(parameterType))
                {
                    exactMatches = -1;
                    break;
                }
            }
            if (exactMatches < 0)
            {
                if (paramsArray == null)
                    continue;

                expandParams = false;
                paramsArray = null;
                goto tryNotExpanding;
            }
            if (exactMatches > bestExactMatches)
            {
                bestExactMatches = exactMatches;
                bestMatch = method;
            }
            else if (exactMatches == bestExactMatches)
            {
                if (method.NumTypeParameters == 0 && bestMatch.NumTypeParameters > 0)
                {
                    bestMatch = method;
                }
            }
        }

        if (bestMatch != null)
            return bestMatch;
        if (candidates.Count <= 1)
            return unresolvedMethodOverload;
        return ambiguousMethodOverload;// candidates[0];
    }

    public override bool IsAccessible(AccessLevelMask accessLevelMask)
    {
        foreach (var method in methods)
            if (method.IsAccessible(accessLevelMask))
                return true;
        return false;
    }

    private Dictionary<string, SD_ConstructedMethodGroup> constructedMethodGroups;
    public SD_ConstructedMethodGroup ConstructMethodGroup(SymbolReference[] typeArgs)
    {
        var delimiter = string.Empty;
        var sb = new StringBuilder();
        if (typeArgs != null)
        {
            foreach (var arg in typeArgs)
            {
                sb.Append(delimiter);
                sb.Append(arg.ToString());
                delimiter = ", ";
            }
        }
        var sig = sb.ToString();

        if (constructedMethodGroups == null)
            constructedMethodGroups = new Dictionary<string, SD_ConstructedMethodGroup>();

        SD_ConstructedMethodGroup result;
        if (constructedMethodGroups.TryGetValue(sig, out result))
        {
            if (result.IsValid() && result.typeArguments != null && result.methods.Count == methods.Count)
            {
                if (result.typeArguments.All(x => x.Definition != null && x.Definition.kind != SymbolKind.Error && x.Definition.IsValid()))
                {
                    result.methods.RemoveWhere(x =>
                        !x.IsValid() ||
                        !methods.Contains(((SD_Method_Constructed)x).genericMethodDefinition));

                    if (methods.Count == result.methods.Count)
                        return result;
                }
            }
        }

        if (result != null)
        {
            foreach (var method in result.methods)
                method.parentSymbol = null;
            result.parentSymbol = null;
        }

        result = new SD_ConstructedMethodGroup(this, typeArgs);
        constructedMethodGroups[sig] = result;
        return result;
    }
}
