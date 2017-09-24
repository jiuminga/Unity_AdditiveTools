using System.Collections.Generic;

public class SD_Type_Parameter : TypeDefinitionBase
{
    public SymbolReference baseTypeConstraint;
    public List<SymbolReference> interfacesConstraint;
    public bool classConstraint;
    public bool structConstraint;
    public bool newConstraint;

    public override string GetTooltipText()
    {
        tooltipText = name + " in " + parentSymbol.GetName();
        if (baseTypeConstraint != null)
            tooltipText += " where " + name + " : " + BaseType().GetName();
        return tooltipText;
    }

    public override string GetName()
    {
        return name;
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        return context.TypeOfTypeParameter(this);
    }

    private bool resolvingBaseType;
    public override TypeDefinitionBase BaseType()
    {
        if (resolvingBaseType)
            return null;
        resolvingBaseType = true;

        if (baseTypeConstraint != null && (baseTypeConstraint.Definition == null || !baseTypeConstraint.Definition.IsValid()) ||
            interfacesConstraint != null && interfacesConstraint.Exists(x => x.Definition == null || !x.Definition.IsValid()))
        {
            baseTypeConstraint = null;
            interfacesConstraint = null;
        }

        if (baseTypeConstraint == null && interfacesConstraint == null)
        {
            interfacesConstraint = new List<SymbolReference>();

            SyntaxTreeNode_Rule clauseNode = null;
            if (declarations != null)
            {
                for (var i = 0; i < declarations.Count; i++)
                {
                    var d = declarations[i];
                    if (d != null && d.IsValid())
                    {
                        SyntaxTreeNode_Rule constraintsNode = null;
                        var typeParameterListNode = d.parseTreeNode.Parent;
                        var parentRuleName = typeParameterListNode.Parent.RuleName;
                        if (parentRuleName == "structDeclaration" ||
                            parentRuleName == "classDeclaration" ||
                            parentRuleName == "interfaceDeclaration" ||
                            parentRuleName == "delegateDeclaration" ||
                            parentRuleName == "interfaceMethodDeclaration")
                        {
                            constraintsNode = typeParameterListNode.Parent.FindChildByName("typeParameterConstraintsClauses") as SyntaxTreeNode_Rule;
                        }
                        else if (parentRuleName == "qidStart" || parentRuleName == "qidPart")
                        {
                            constraintsNode = typeParameterListNode.Parent
                                .Parent // qid
                                .Parent // memberName
                                .Parent // methodHeader
                                .FindChildByName("typeParameterConstraintsClauses") as SyntaxTreeNode_Rule;
                        }

                        if (constraintsNode != null)
                        {
                            for (var j = 0; j < constraintsNode.NumValidNodes; j++)
                            {
                                clauseNode = constraintsNode.NodeAt(j);
                                if (clauseNode != null && clauseNode.NumValidNodes == 4)
                                {
                                    var c = clauseNode.NodeAt(1);
                                    if (c != null && c.NumValidNodes == 1)
                                    {
                                        var id = DecodeId(c.LeafAt(0).token.text);
                                        if (id == name)
                                            break;
                                    }
                                }
                                clauseNode = null;
                            }
                        }
                        break;
                    }
                }
            }

            if (clauseNode != null)
            {
                var constrantListNode = clauseNode.NodeAt(3);
                if (constrantListNode != null)
                {
                    var secondaryList = constrantListNode.NodeAt(-1);
                    if (secondaryList != null && secondaryList.RuleName == "secondaryConstraintList")
                    {
                        for (int i = 0; i < secondaryList.NumValidNodes; i += 2)
                        {
                            var constraintNode = secondaryList.NodeAt(i);
                            if (constraintNode != null)
                            {
                                var typeNameNode = constraintNode.NodeAt(0);
                                if (typeNameNode != null)
                                {
                                    if (baseTypeConstraint == null && interfacesConstraint.Count == 0)
                                    {
                                        var resolvedType = ResolveNode(typeNameNode, null, null, 0, true) as TypeDefinitionBase;
                                        if (resolvedType != null && resolvedType.kind != SymbolKind.Error)
                                        {
                                            if (resolvedType.kind == SymbolKind.Interface)
                                                interfacesConstraint.Add(new SymbolReference(typeNameNode));
                                            else
                                                baseTypeConstraint = new SymbolReference(typeNameNode);
                                        }
                                    }
                                    else
                                    {
                                        interfacesConstraint.Add(new SymbolReference(typeNameNode));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        var result = baseTypeConstraint != null ? baseTypeConstraint.Definition as TypeDefinitionBase : base.BaseType();
        if (result == this)
        {
            baseTypeConstraint = new SymbolReference(circularBaseType);
            result = circularBaseType;
        }
        resolvingBaseType = false;
        return result;
    }

    public override List<SymbolReference> Interfaces()
    {
        if (interfacesConstraint == null)
            BaseType();
        return interfacesConstraint;
    }

    internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        if (this == typeArgument)
            return argumentType;

        return null;
    }
}
