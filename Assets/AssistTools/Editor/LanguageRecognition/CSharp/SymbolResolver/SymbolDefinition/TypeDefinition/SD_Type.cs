using System;
using System.Collections.Generic;
using System.Text;

public class SD_Type : TypeDefinitionBase
{
    protected SymbolReference baseType;
    protected List<SymbolReference> interfaces;

    public List<SD_Type_Parameter> typeParameters;

    private Dictionary<string, SD_Type_Constructed> constructedTypes;
    public virtual SD_Type_Constructed ConstructType(SymbolReference[] typeArgs)
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

        if (constructedTypes == null)
            constructedTypes = new Dictionary<string, SD_Type_Constructed>();

        SD_Type_Constructed result;

        result = new SD_Type_Constructed(this, typeArgs);
        constructedTypes[sig] = result;
        return result;
    }

    public override SymbolDefinition TypeOf()
    {
        return this;
    }

    public override void InvalidateBaseType()
    {
        baseType = null;
        interfaces = null;
        ++LR_SyntaxTree.resolverVersion;
        if (LR_SyntaxTree.resolverVersion == 0)
            ++LR_SyntaxTree.resolverVersion;
    }

    public override List<SymbolReference> Interfaces()
    {
        if (interfaces == null)
            BaseType();
        return interfaces;
    }

    private bool resolvingBaseType = false;
    public override TypeDefinitionBase BaseType()
    {
        if (resolvingBaseType)
            return null;
        resolvingBaseType = true;

        if (baseType != null && (baseType.Definition == null || !baseType.Definition.IsValid()) ||
            interfaces != null && interfaces.Exists(x => x.Definition == null || !x.Definition.IsValid()))
        {
            baseType = null;
            interfaces = null;
        }

        if (baseType == null && interfaces == null)
        {
            interfaces = new List<SymbolReference>();

            SyntaxTreeNode_Rule baseNode = null;
            SyntaxTreeNode_Rule interfaceListNode = null;
            SymbolDeclaration decl = null;
            if (declarations != null)
            {
                foreach (var d in declarations)
                {
                    if (d != null)
                    {
                        baseNode = (SyntaxTreeNode_Rule)d.parseTreeNode.FindChildByName(
                            d.kind == SymbolKind.Class ? "classBase" :
                            d.kind == SymbolKind.Struct ? "structInterfaces" :
                            "interfaceBase");
                        interfaceListNode = baseNode != null ? baseNode.NodeAt(1) : null;

                        if (baseNode != null)
                        {
                            decl = d;
                            break;
                        }
                    }
                }
            }

            if (decl != null)
            {
                switch (decl.kind)
                {
                    case SymbolKind.Class:
                        if (interfaceListNode != null)
                        {
                            baseType = new SymbolReference(interfaceListNode.ChildAt(0));
                            if (baseType.Definition.kind == SymbolKind.Interface)
                            {
                                interfaces.Add(baseType);
                                baseType = this != builtInTypes_object ? ReflectedTypeReference.ForType(typeof(object)) : null;
                            }

                            for (var i = 2; i < interfaceListNode.NumValidNodes; i += 2)
                                interfaces.Add(new SymbolReference(interfaceListNode.ChildAt(i)));
                        }
                        else
                        {
                            baseType = this != builtInTypes_object ? ReflectedTypeReference.ForType(typeof(object)) : null;
                        }
                        break;

                    case SymbolKind.Struct:
                    case SymbolKind.Interface:
                        baseType = decl.kind == SymbolKind.Struct ?
                            ReflectedTypeReference.ForType(typeof(ValueType)) :
                            ReflectedTypeReference.ForType(typeof(object));
                        if (interfaceListNode != null)
                        {
                            for (var i = 0; i < interfaceListNode.NumValidNodes; i += 2)
                                interfaces.Add(new SymbolReference(interfaceListNode.ChildAt(i)));
                        }
                        break;

                    case SymbolKind.Enum:
                        baseType = ReflectedTypeReference.ForType(typeof(Enum));
                        break;

                    case SymbolKind.Delegate:
                        baseType = ReflectedTypeReference.ForType(typeof(MulticastDelegate));
                        break;
                }
            }
            //Debug.Log("BaseType() of " + this + " is " + (baseType != null ? baseType.definition.ToString() : "null"));
        }

        var result = baseType != null ? baseType.Definition as TypeDefinitionBase : base.BaseType();
        if (result == this)
        {
            baseType = new SymbolReference(circularBaseType);
            result = circularBaseType;
        }
        resolvingBaseType = false;
        return result;
    }

    public override TypeDefinitionBase ConvertTo(TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return null;

        if (otherType is SD_Type_Parameter)
            return this;

        if (otherType == builtInTypes_object)
            return otherType;

        if (otherType.GetGenericSymbol() == builtInTypes_Nullable)
        {
            var otherTypeAsConstructedType = otherType as SD_Type_Constructed;
            if (otherTypeAsConstructedType != null && otherTypeAsConstructedType.typeArguments[0].Definition == this)
                return otherType;
        }

        if (this == builtInTypes_int && (
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_uint && (
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_byte && (
            otherType == builtInTypes_short ||
            otherType == builtInTypes_ushort ||
            otherType == builtInTypes_int ||
            otherType == builtInTypes_uint ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_sbyte && (
            otherType == builtInTypes_short ||
            otherType == builtInTypes_int ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_short && (
            otherType == builtInTypes_int ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_ushort && (
            otherType == builtInTypes_int ||
            otherType == builtInTypes_uint ||
            otherType == builtInTypes_long ||
            otherType == builtInTypes_ulong ||
            otherType == builtInTypes_float ||
            otherType == builtInTypes_double))
            return otherType;
        if ((this == builtInTypes_long || this == builtInTypes_ulong) &&
        (otherType == builtInTypes_float || otherType == builtInTypes_double))
            return otherType;
        if (this == builtInTypes_float &&
            otherType == builtInTypes_double)
            return otherType;
        if (this == builtInTypes_char &&
            otherType == builtInTypes_string)
            return otherType;

        var otherTypeAsConstructed = otherType as SD_Type_Constructed;
        if (otherTypeAsConstructed != null)
            otherType = otherTypeAsConstructed.genericTypeDefinition;

        if (this == otherType)
            return this;

        if (convertingToBase)
            return null;
        convertingToBase = true;

        var baseTypeDefinition = BaseType();

        if (interfaces != null && (otherType.kind == SymbolKind.Interface || otherType.kind == SymbolKind.TypeParameter))
        {
            for (var i = 0; i < interfaces.Count; ++i)
            {
                var interfaceDefinition = interfaces[i].Definition as TypeDefinitionBase;
                if (interfaceDefinition != null)
                {
                    var convertedInterface = interfaceDefinition.ConvertTo(otherType);
                    if (convertedInterface != null)
                    {
                        convertingToBase = false;
                        return convertedInterface;
                    }
                }
            }
        }

        if (baseTypeDefinition != null)
        {
            var convertedBase = baseTypeDefinition.ConvertTo(otherType);
            convertingToBase = false;
            return convertedBase;
        }

        convertingToBase = false;
        return null;
    }

    private bool checkingDerivesFromBase;
    public override bool DerivesFromRef(ref TypeDefinitionBase otherType)
    {
        if (otherType == null)
            return false;

        var otherTypeAsConstructed = otherType as SD_Type_Constructed;
        if (otherTypeAsConstructed != null)
            otherType = otherTypeAsConstructed.genericTypeDefinition;

        if (this == otherType)
            return true;

        if (interfaces == null)
            BaseType();

        if (checkingDerivesFromBase)
            return false;
        checkingDerivesFromBase = true;

        if (interfaces != null)
            for (var i = 0; i < interfaces.Count; ++i)
            {
                var typeDefinition = interfaces[i].Definition as TypeDefinitionBase;
                if (typeDefinition != null && typeDefinition.DerivesFromRef(ref otherType))
                {
                    checkingDerivesFromBase = false;
                    return true;
                }
            }

        if (BaseType() != null)
        {
            var result = BaseType().DerivesFromRef(ref otherType);
            checkingDerivesFromBase = false;
            return result;
        }

        checkingDerivesFromBase = false;
        return false;
    }

    public override SymbolDefinition AddDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind != SymbolKind.TypeParameter)
            return base.AddDeclaration(symbol);

        var symbolName = symbol.ReflectionName;// symbol.Name;
        if (typeParameters == null)
            typeParameters = new List<SD_Type_Parameter>();
        var definition = typeParameters.Find(x => x.name == symbolName);
        if (definition == null)
        {
            definition = (SD_Type_Parameter)Create(symbol);
            definition.parentSymbol = this;
            typeParameters.Add(definition);
        }

        symbol.definition = definition;

        var nameNode = symbol.NameNode();
        if (nameNode != null)
        {
            var leaf = nameNode as SyntaxTreeNode_Leaf;
            if (leaf != null)
                leaf.SetDeclaredSymbol(definition);
            else
            {
                // TODO: Remove this block?
                var lastLeaf = ((SyntaxTreeNode_Rule)nameNode).GetLastLeaf();
                if (lastLeaf != null)
                {
                    if (lastLeaf.Parent.RuleName == "typeParameterList")
                        lastLeaf = lastLeaf.Parent.Parent.LeafAt(0);
                    lastLeaf.SetDeclaredSymbol(definition);
                }
            }
        }

        //// this.ReflectionName has changed
        //parentSymbol.members.Remove(this);
        //parentSymbol.members[ReflectionName] = this;

        return definition;
    }

    public override void RemoveDeclaration(SymbolDeclaration symbol)
    {
        if (symbol.kind == SymbolKind.TypeParameter && typeParameters != null)
        {
            if (typeParameters.Remove(symbol.definition as SD_Type_Parameter))
            {
                //// this.ReflectionName has changed
                //parentSymbol.members.Remove(this);
                //parentSymbol.members[ReflectionName] = this;
            }
        }

        base.RemoveDeclaration(symbol);
    }

    public override SymbolDefinition FindName(string memberName, int numTypeParameters, bool asTypeOnly)
    {
        memberName = DecodeId(memberName);

        if (numTypeParameters == 0 && typeParameters != null)
        {
            for (var i = typeParameters.Count; i-- > 0; )
                if (typeParameters[i].name == memberName)
                    return typeParameters[i];
        }

        var member = base.FindName(memberName, numTypeParameters, asTypeOnly);
        return member;
    }

    public override List<SD_Type_Parameter> GetTypeParameters()
    {
        return typeParameters;
    }

    public override string GetTooltipText()
    {
        if (kind == SymbolKind.Delegate)
            return base.GetTooltipText();

        //	if (tooltipText != null)
        //		return tooltipText;

        var parentSD = parentSymbol;
        if (parentSD != null && !string.IsNullOrEmpty(parentSD.GetName()))
            tooltipText = kind.ToString().ToLowerInvariant() + " " + parentSD.GetName() + "." + name;
        else
            tooltipText = kind.ToString().ToLowerInvariant() + " " + name;

        if (typeParameters != null)
        {
            tooltipText += "<" + TypeOfTypeParameter(typeParameters[0]).GetName();
            for (var i = 1; i < typeParameters.Count; ++i)
                tooltipText += ", " + TypeOfTypeParameter(typeParameters[i]).GetName();
            tooltipText += '>';
        }

        var xmlDocs = GetXmlDocs();
        if (!string.IsNullOrEmpty(xmlDocs))
        {
            tooltipText += "\n\n" + xmlDocs;
        }

        return tooltipText;
    }

    public override TypeDefinitionBase SubstituteTypeParameters(SymbolDefinition context)
    {
        if (typeParameters == null)
            return base.SubstituteTypeParameters(context);

        var constructType = false;
        var typeArguments = new SymbolReference[typeParameters.Count];
        for (var i = 0; i < typeArguments.Length; ++i)
        {
            typeArguments[i] = new SymbolReference(typeParameters[i]);
            var original = typeParameters[i];
            if (original == null)
                continue;
            var substitute = original.SubstituteTypeParameters(context);
            if (substitute != original)
            {
                typeArguments[i] = new SymbolReference(substitute);
                constructType = true;
            }
        }
        if (!constructType)
            return this;
        return ConstructType(typeArguments);
    }

    internal override TypeDefinitionBase BindTypeArgument(TypeDefinitionBase typeArgument, TypeDefinitionBase argumentType)
    {
        if (NumTypeParameters == 0)
            return base.BindTypeArgument(typeArgument, argumentType);

        if (argumentType.kind == SymbolKind.LambdaExpression)
            return argumentType.BindTypeArgument(typeArgument, TypeOf() as TypeDefinitionBase);

        TypeDefinitionBase convertedArgument = this;
        if (!argumentType.DerivesFromRef(ref convertedArgument))
            return base.BindTypeArgument(typeArgument, argumentType);

        var argumentAsConstructedType = convertedArgument as SD_Type_Constructed;
        if (argumentAsConstructedType != null && GetGenericSymbol() == argumentAsConstructedType.GetGenericSymbol())
        {
            TypeDefinitionBase inferedType = null;
            for (int i = 0; i < NumTypeParameters; ++i)
            {
                var fromConstructedType = argumentAsConstructedType.typeArguments[i].Definition as TypeDefinitionBase;
                if (fromConstructedType != null)
                {
                    var boundTypeArgument = typeParameters[i].BindTypeArgument(typeArgument, fromConstructedType);
                    if (boundTypeArgument != null)
                    {
                        if (inferedType == null || inferedType.CanConvertTo(boundTypeArgument))
                            inferedType = boundTypeArgument;
                        else if (!boundTypeArgument.CanConvertTo(inferedType))
                            return null;
                    }
                }
            }

            if (inferedType != null)
                return inferedType;
        }
        return base.BindTypeArgument(typeArgument, argumentType);
    }
}

