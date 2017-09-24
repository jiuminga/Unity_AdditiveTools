using System;
using System.Collections.Generic;
using System.Text;
public class Declaration_Namespace : SymbolDeclaration
{
    public List<SymbolReference> importedNamespaces = new List<SymbolReference>();
    public List<TypeAlias> typeAliases = new List<TypeAlias>();

    public Declaration_Namespace(string nsName)
        : base(nsName)
    { }

    public Declaration_Namespace() { }

    public void ImportNamespace(string namespaceToImport, SyntaxTreeNode_Base declaringNode)
    {
        throw new NotImplementedException();
    }

    protected override void Dump(StringBuilder sb, string indent)
    {
        base.Dump(sb, indent);

        sb.AppendLine(indent + "Imports:");
        var indent2 = indent + "  ";
        foreach (var ns in importedNamespaces)
            sb.AppendLine(indent2 + ns);

        sb.AppendLine("  Aliases:");
        foreach (var ta in typeAliases)
            sb.AppendLine(indent2 + ta.name);
    }
}

