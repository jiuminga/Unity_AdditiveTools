using System.Collections.Generic;
using System.Linq;
using System.Reflection;
public class Scope_Namespace : Scope_Base
{
    public Declaration_Namespace declaration;
    public SD_NameSpace definition;

    public List<SymbolDeclaration> typeDeclarations;

    public Scope_Namespace(SyntaxTreeNode_Rule node) : base(node) { }

    public override IEnumerable<SD_NameSpace> VisibleNamespacesInScope()
    {
        yield return definition;

        foreach (var nsRef in declaration.importedNamespaces)
        {
            var ns = nsRef.Definition as SD_NameSpace;
            if (ns != null)
                yield return ns;
        }

        if (parentScope != null)
            foreach (var ns in parentScope.VisibleNamespacesInScope())
                yield return ns;
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (definition == null)
            return null;

        symbol.scope = this;

        if (symbol.kind == SymbolKind.Class ||
            symbol.kind == SymbolKind.Struct ||
            symbol.kind == SymbolKind.Interface ||
            symbol.kind == SymbolKind.Enum ||
            symbol.kind == SymbolKind.Delegate)
        {
            if (typeDeclarations == null)
                typeDeclarations = new List<SymbolDeclaration>();
            typeDeclarations.Add(symbol);
        }

        if (symbol.kind == SymbolKind.ImportedNamespace)
        {
            declaration.importedNamespaces.Add(new SymbolReference(symbol.parseTreeNode.ChildAt(0)));
            return null;
        }
        else if (symbol.kind == SymbolKind.TypeAlias)
        {
            declaration.typeAliases.Add(new TypeAlias
            {
                name = symbol.parseTreeNode.ChildAt(0).Print(),
                type = new SymbolReference(symbol.parseTreeNode.ChildAt(2)),
                declaration = symbol
            });
            return null;
        }

        return definition.AddDeclaration(symbol);
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (typeDeclarations != null)
            typeDeclarations.Remove(symbol);

        if (symbol.kind == SymbolKind.ImportedNamespace)
        {
            var node = symbol.parseTreeNode;
            declaration.importedNamespaces.RemoveAll(x => x.Node.Parent == node);
            return;
        }
        else if (symbol.kind == SymbolKind.TypeAlias)
        {
            var index = declaration.typeAliases.FindIndex(x => x.declaration == symbol);
            if (index >= 0)
                declaration.typeAliases.RemoveAt(index);
            return;
        }

        if (definition != null)
            definition.RemoveDeclaration(symbol);
    }

    public override void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;

        var id = SymbolDefinition.DecodeId(leaf.token.text);

        for (int i = declaration.typeAliases.Count; i-- > 0;)
        {
            if (declaration.typeAliases[i].name == id)
            {
                if (declaration.typeAliases[i].type != null)
                {
                    leaf.ResolvedSymbol = declaration.typeAliases[i].type.Definition;
                    return;
                }
                else
                {
                    break;
                }
            }
        }

        if (leaf.ResolvedSymbol == null)
        {
            for (var i = declaration.importedNamespaces.Count; i-- > 0;)
            {
                var nsRef = declaration.importedNamespaces[i];
                if (nsRef.IsBefore(leaf) && nsRef.Definition != null)
                {
                    nsRef.Definition.ResolveMember(leaf, this, numTypeArgs, true);
                    if (leaf.ResolvedSymbol != null)
                    {
                        if (leaf.ResolvedSymbol.kind == SymbolKind.Namespace)
                            leaf.ResolvedSymbol = null;
                        else
                            break;
                    }
                }
            }
        }

        var parentScopeDef = parentScope != null ? ((Scope_Namespace)parentScope).definition : null;
        for (var nsDef = definition;
            leaf.ResolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
            nsDef = nsDef.parentSymbol as SD_NameSpace)
        {
            nsDef.ResolveMember(leaf, this, numTypeArgs, true);
        }

        if (leaf.ResolvedSymbol == null && parentScope != null)
            parentScope.Resolve(leaf, numTypeArgs, true);
    }

    public override void ResolveAttribute(SyntaxTreeNode_Leaf leaf)
    {
        leaf.ResolvedSymbol = null;

        var id = SymbolDefinition.DecodeId(leaf.token.text);

        for (int i = declaration.typeAliases.Count; i-- > 0;)
        {
            if (declaration.typeAliases[i].name == id)
            {
                if (declaration.typeAliases[i].type != null)
                {
                    leaf.ResolvedSymbol = declaration.typeAliases[i].type.Definition;
                    return;
                }
                else
                {
                    break;
                }
            }
        }

        var parentScopeDef = parentScope != null ? ((Scope_Namespace)parentScope).definition : null;
        for (var nsDef = definition;
            leaf.ResolvedSymbol == null && nsDef != null && nsDef != parentScopeDef;
            nsDef = nsDef.parentSymbol as SD_NameSpace)
        {
            nsDef.ResolveAttributeMember(leaf, this);
        }

        if (leaf.ResolvedSymbol == null)
        {
            foreach (var nsRef in declaration.importedNamespaces)
            {
                if (nsRef.IsBefore(leaf) && nsRef.Definition != null)
                {
                    nsRef.Definition.ResolveAttributeMember(leaf, this);
                    if (leaf.ResolvedSymbol != null)
                        break;
                }
            }
        }

        if (leaf.ResolvedSymbol == null && parentScope != null)
            parentScope.ResolveAttribute(leaf);
    }

    public override SymbolDefinition ResolveAsExtensionMethod(SyntaxTreeNode_Leaf invokedLeaf, SymbolDefinition invokedSymbol, TypeDefinitionBase memberOf, SyntaxTreeNode_Rule argumentListNode, SymbolReference[] typeArgs, Scope_Base context)
    {
        if (invokedLeaf == null && (invokedSymbol == null || invokedSymbol.kind == SymbolKind.Error))
            return null;

        var id = invokedSymbol != null && invokedSymbol.kind != SymbolKind.Error ? invokedSymbol.name : invokedLeaf != null ? SymbolDefinition.DecodeId(invokedLeaf.token.text) : "";

        int numArguments = 1;
        Modifiers[] modifiers = null;
        List<TypeDefinitionBase> argumentTypes = null;

        MethodDefinition firstAccessibleMethod = null;

        var thisAssembly = GetAssembly();

        var extensionsMethods = new HashSet<MethodDefinition>();

        thisAssembly.CollectExtensionMethods(definition, id, typeArgs, memberOf, extensionsMethods, context);
        if (extensionsMethods.Count > 0)
        {
            firstAccessibleMethod = extensionsMethods.First();

            if (argumentTypes == null)
                numArguments = SD_MethodGroup.ProcessArgumentListNode(argumentListNode, out modifiers, out argumentTypes, memberOf);

            var candidates = new List<MethodDefinition>(extensionsMethods.Count);
            foreach (var method in extensionsMethods)
                if (argumentTypes == null || method.CanCallWith(modifiers, true))
                    candidates.Add(method);

            if (typeArgs == null)
            {
                for (var i = candidates.Count; i-- > 0;)
                {
                    var candidate = candidates[i];
                    if (candidate.NumTypeParameters == 0 || argumentTypes == null)
                        continue;

                    candidate = SD_MethodGroup.InferMethodTypeArguments(candidate, argumentTypes, invokedLeaf);
                    if (candidate == null)
                        candidates.RemoveAt(i);
                    else
                        candidates[i] = candidate;
                }
            }

            var resolved = SD_MethodGroup.ResolveMethodOverloads(numArguments, argumentTypes, modifiers, candidates);
            if (resolved != null && resolved.kind != SymbolKind.Error)
                return resolved;
        }

        extensionsMethods.Clear();

        var importedNamespaces = declaration.importedNamespaces;
        for (var i = importedNamespaces.Count; i-- > 0;)
        {
            var nsDef = importedNamespaces[i].Definition as SD_NameSpace;
            if (nsDef != null)
                thisAssembly.CollectExtensionMethods(nsDef, id, typeArgs, memberOf, extensionsMethods, context);
        }
        if (extensionsMethods.Count > 0)
        {
            if (firstAccessibleMethod == null)
                firstAccessibleMethod = extensionsMethods.First();

            if (argumentTypes == null)
                numArguments = SD_MethodGroup.ProcessArgumentListNode(argumentListNode, out modifiers, out argumentTypes, memberOf);

            var candidates = new List<MethodDefinition>(extensionsMethods.Count);
            foreach (var method in extensionsMethods)
                if (argumentTypes == null || method.CanCallWith(modifiers, true))
                    candidates.Add(method);

            if (typeArgs == null)
            {
                for (var i = candidates.Count; i-- > 0;)
                {
                    var candidate = candidates[i];
                    if (candidate.NumTypeParameters == 0 || argumentTypes == null)
                        continue;

                    candidate = SD_MethodGroup.InferMethodTypeArguments(candidate, argumentTypes, invokedLeaf);
                    if (candidate == null)
                        candidates.RemoveAt(i);
                    else
                        candidates[i] = candidate;
                }
            }

            var resolved = SD_MethodGroup.ResolveMethodOverloads(numArguments, argumentTypes, modifiers, candidates);
            if (resolved != null && resolved.kind != SymbolKind.Error)
                return resolved;
        }

        if (parentScope != null)
        {
            var resolved = parentScope.ResolveAsExtensionMethod(invokedLeaf, invokedSymbol, memberOf, argumentListNode, typeArgs, context);
            if (resolved != null)
                return resolved;
        }

        if (firstAccessibleMethod != null)
        {
            invokedLeaf.ResolvedSymbol = firstAccessibleMethod;
            invokedLeaf.m_sSemanticError = SD_MethodGroup.unresolvedMethodOverload.name;
        }
        return null;
    }

    public override SymbolDefinition FindName(string symbolName, int numTypeParameters)
    {
        return definition.FindName(symbolName, numTypeParameters, true);
    }

    public override void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        assembly = GetAssembly();

        definition.GetMembersCompletionData(data, BindingFlags.NonPublic, AccessLevelMask.Any, assembly);

        foreach (var ta in declaration.typeAliases)
            if (!data.ContainsKey(ta.name))
                data.Add(ta.name, ta.type.Definition);

        foreach (var i in declaration.importedNamespaces)
        {
            var nsDef = i.Definition as SD_NameSpace;
            if (nsDef != null)
                nsDef.GetTypesOnlyCompletionData(data, AccessLevelMask.Any, assembly);
        }

        var parentScopeDef = parentScope != null ? ((Scope_Namespace)parentScope).definition : null;
        for (var nsDef = definition.parentSymbol;
            nsDef != null && nsDef != parentScopeDef;
            nsDef = nsDef.parentSymbol as SD_NameSpace)
        {
            nsDef.GetCompletionData(data, fromInstance, assembly);
        }

        base.GetCompletionData(data, false, assembly);
    }

    public override SD_Type EnclosingType()
    {
        return null;
    }

    public override void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data)
    {
        var assembly = this.GetAssembly();

        assembly.GetExtensionMethodsCompletionData(forType, definition, data);
        foreach (var nsRef in declaration.importedNamespaces)
        {
            var ns = nsRef.Definition as SD_NameSpace;
            if (ns != null)
                assembly.GetExtensionMethodsCompletionData(forType, ns, data);
        }

        if (parentScope != null)
            parentScope.GetExtensionMethodsCompletionData(forType, data);
    }
}

