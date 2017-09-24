using System;
using System.Collections.Generic;

public abstract class Scope_Base
{
    public static SyntaxTreeNode_Base completionNode;
    public static string completionAssetPath;
    public static int completionAtLine;
    public static int completionAtTokenIndex;

    protected SyntaxTreeNode_Rule parseTreeNode;

    public Scope_Base(SyntaxTreeNode_Rule node)
    {
        parseTreeNode = node;
    }

    public Scope_Base _parentScope;
    public Scope_Base parentScope
    {
        get
        {
            if (_parentScope != null || parseTreeNode == null)
                return _parentScope;
            for (var node = parseTreeNode.Parent; node != null; node = node.Parent)
                if (node.scope != null)
                    return node.scope;
            return null;
        }
        set { _parentScope = value; }
    }

    public SD_Assembly GetAssembly()
    {
        for (Scope_Base scope = this; scope != null; scope = scope.parentScope)
        {
            var cuScope = scope as Scope_CompilationUnit;
            if (cuScope != null)
                return cuScope.assembly;
        }
        throw new Exception("No Assembly for scope???");
    }

    public abstract SymbolDefinition AddDeclaration(SymbolDeclaration symbol);

    public abstract void RemoveDeclaration(SymbolDeclaration symbol);

    public virtual string CreateAnonymousName()
    {
        return parentScope != null ? parentScope.CreateAnonymousName() : null;
    }

    public virtual void Resolve(SyntaxTreeNode_Leaf leaf, int numTypeArgs, bool asTypeOnly)
    {
        leaf.ResolvedSymbol = null;
        if (parentScope != null)
            parentScope.Resolve(leaf, numTypeArgs, asTypeOnly);
    }

    public virtual void ResolveAttribute(SyntaxTreeNode_Leaf leaf)
    {
        leaf.ResolvedSymbol = null;
        if (parentScope != null)
            parentScope.ResolveAttribute(leaf);
    }

    public virtual SymbolDefinition ResolveAsExtensionMethod(SyntaxTreeNode_Leaf invokedLeaf, SymbolDefinition invokedSymbol, TypeDefinitionBase memberOf, SyntaxTreeNode_Rule argumentListNode, SymbolReference[] typeArgs, Scope_Base context)
    {
        return parentScope != null ? parentScope.ResolveAsExtensionMethod(invokedLeaf, invokedSymbol, memberOf, argumentListNode, typeArgs, context) : null;
    }

    public abstract SymbolDefinition FindName(string symbolName, int numTypeParameters);

    public virtual void GetCompletionData(Dictionary<string, SymbolDefinition> data, bool fromInstance, SD_Assembly assembly)
    {
        if (parentScope != null)
            parentScope.GetCompletionData(data, fromInstance, assembly);
    }

    public virtual SD_Type EnclosingType()
    {
        return parentScope != null ? parentScope.EnclosingType() : null;
    }

    public Scope_Namespace EnclosingNamespaceScope()
    {
        for (var parent = parentScope; parent != null; parent = parent.parentScope)
        {
            var parentNamespaceScope = parent as Scope_Namespace;
            if (parentNamespaceScope != null)
                return parentNamespaceScope;
        }
        return null;
    }

    public virtual void GetExtensionMethodsCompletionData(TypeDefinitionBase forType, Dictionary<string, SymbolDefinition> data)
    {
        if (parentScope != null)
            parentScope.GetExtensionMethodsCompletionData(forType, data);
    }

    public virtual IEnumerable<SD_NameSpace> VisibleNamespacesInScope()
    {
        if (parentScope != null)
            foreach (var ns in parentScope.VisibleNamespacesInScope())
                yield return ns;
    }
}

