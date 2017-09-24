//TODO: Finish this
public class SD_type_LambaExpr : TypeDefinitionBase
{
    public override bool CanConvertTo(TypeDefinitionBase otherType)
    {
        if (ConvertTo(otherType) != null)
            return true;
        return false;
    }

    public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType.kind != SymbolKind.Delegate)
            return null;

        var declaration = declarations.FirstOrDefault();
        if (declaration == null)
            return null;

        if (declaration.parseTreeNode.NumValidNodes == 0)
            return null;

        var delegateParameters = otherType.GetParameters();
        var numDelegateParameters = delegateParameters != null ? delegateParameters.Count : 0;

        var anonymousFunctionSignatureNode = declaration.parseTreeNode.NodeAt(0);
        if (anonymousFunctionSignatureNode.NumValidNodes == 1 && anonymousFunctionSignatureNode.NodeAt(0) != null)
        {
            // there is one parameter
            if (numDelegateParameters == 1)
                return otherType;
        }
        else
        {
            var parameterListNode =
                (anonymousFunctionSignatureNode.FindChildByName("implicitAnonymousFunctionParameterList") ??
                anonymousFunctionSignatureNode.FindChildByName("explicitAnonymousFunctionParameterList")) as SyntaxTreeNode_Rule;
            var numLambdaParameters = parameterListNode == null ? 0 : (parameterListNode.NumValidNodes + 1) / 2;
            if (numDelegateParameters == numLambdaParameters)
                return otherType;
        }

        return null;
    }

    private new SymbolDefinition TypeOf()
    {
        var declaration = declarations.FirstOrDefault();
        if (declaration == null)
            return unknownType;
        if (declaration.parseTreeNode.NumValidNodes != 3)
            return unknownType;
        var lambdaExpressionBodyNode = declaration.parseTreeNode.NodeAt(2);
        var resolvedExpression = ResolveNode(lambdaExpressionBodyNode);
        var returnType = resolvedExpression == null ? unknownType : resolvedExpression.TypeOf();
        return returnType;
    }

    internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        var returnType = TypeOf() as TypeDefinitionBase;
        if (returnType != null && returnType.kind != SymbolKind.Error)
        {
            var boundReturnType = argumentType.BindTypeArgument(typeArgument, returnType);
            return boundReturnType;
        }
        return null;
    }
}
