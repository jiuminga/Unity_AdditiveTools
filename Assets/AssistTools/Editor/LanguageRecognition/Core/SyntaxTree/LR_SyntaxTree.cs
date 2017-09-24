using System.Text;

public partial class LR_SyntaxTree
{
    public static uint resolverVersion = 2;

    public SyntaxTreeNode_Rule root;

    public override string ToString()
    {
        var sb = new StringBuilder();
        root.Dump(sb, 0);
        return sb.ToString();
    }
}


