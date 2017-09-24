using System;

public enum SemanticFlags
{
    None = 0,

    SymbolDeclarationsMask = (1 << 8) - 1,
    ScopesMask = ~SymbolDeclarationsMask,

    SymbolDeclarationsBegin = 1,

    Declaration_Namespace,
    UsingNamespace,
    UsingAlias,
    ExternAlias,
    ClassDeclaration,
    TypeParameterDeclaration,
    BaseListDeclaration,
    ConstructorDeclarator,
    DestructorDeclarator,
    ConstantDeclarator,
    MethodDeclarator,
    LocalVariableDeclarator,
    ForEachVariableDeclaration,
    FromClauseVariableDeclaration,
    LabeledStatement,
    CatchExceptionParameterDeclaration,
    FixedParameterDeclaration,
    ParameterArrayDeclaration,
    ImplicitParameterDeclaration,
    ExplicitParameterDeclaration,
    PropertyDeclaration,
    IndexerDeclaration,
    GetAccessorDeclaration,
    SetAccessorDeclaration,
    EventDeclarator,
    EventWithAccessorsDeclaration,
    AddAccessorDeclaration,
    RemoveAccessorDeclaration,
    VariableDeclarator,
    OperatorDeclarator,
    ConversionOperatorDeclarator,
    StructDeclaration,
    InterfaceDeclaration,
    InterfacePropertyDeclaration,
    InterfaceMethodDeclaration,
    InterfaceEventDeclaration,
    InterfaceIndexerDeclaration,
    InterfaceGetAccessorDeclaration,
    InterfaceSetAccessorDeclaration,
    EnumDeclaration,
    EnumMemberDeclaration,
    DelegateDeclaration,
    AnonymousObjectCreation,
    MemberDeclarator,
    LambdaExpressionDeclaration,
    AnonymousMethodDeclaration,

    SymbolDeclarationsEnd,


    ScopesBegin = 1 << 8,

    CompilationUnitScope = 1 << 8,
    NamespaceBodyScope = 2 << 8,
    ClassBaseScope = 3 << 8,
    TypeParameterConstraintsScope = 4 << 8,
    ClassBodyScope = 5 << 8,
    StructInterfacesScope = 6 << 8,
    StructBodyScope = 7 << 8,
    InterfaceBaseScope = 8 << 8,
    InterfaceBodyScope = 9 << 8,
    FormalParameterListScope = 10 << 8,
    EnumBaseScope = 11 << 8,
    EnumBodyScope = 12 << 8,
    MethodBodyScope = 13 << 8,
    ConstructorInitializerScope = 14 << 8,
    LambdaExpressionScope = 15 << 8,
    LambdaExpressionBodyScope = 16 << 8,
    AnonymousMethodScope = 17 << 8,
    AnonymousMethodBodyScope = 18 << 8,
    CodeBlockScope = 19 << 8,
    SwitchBlockScope = 20 << 8,
    ForStatementScope = 21 << 8,
    EmbeddedStatementScope = 22 << 8,
    UsingStatementScope = 23 << 8,
    LocalVariableInitializerScope = 24 << 8,
    SpecificCatchScope = 25 << 8,
    ArgumentListScope = 26 << 8,
    AttributeArgumentsScope = 27 << 8,
    MemberInitializerScope = 28 << 8,

    TypeDeclarationScope = 29 << 8,
    MethodDeclarationScope = 30 << 8,
    AttributesScope = 31 << 8,
    AccessorBodyScope = 32 << 8,
    AccessorsListScope = 33 << 8,
    QueryExpressionScope = 34 << 8,
    QueryBodyScope = 35 << 8,
    MemberDeclarationScope = 36 << 8,

    ScopesEnd,
}

public interface IVisitableTreeNode<NonLeaf, Leaf>
{
    bool Accept(IHierarchicalVisitor<NonLeaf, Leaf> visitor);
}

public interface IHierarchicalVisitor<NonLeaf, Leaf>
{
    bool Visit(Leaf leafNode);
    bool VisitEnter(NonLeaf nonLeafNode);
    bool VisitLeave(NonLeaf nonLeafNode);
}

[Flags]
public enum IdentifierCompletionsType
{
    None = 1 << 0,
    Namespace = 1 << 1,
    TypeName = 1 << 2,
    ArrayType = 1 << 3,
    NonArrayType = 1 << 4,
    ValueType = 1 << 5,
    SimpleType = 1 << 6,
    ExceptionClassType = 1 << 7,
    AttributeClassType = 1 << 8,
    Member = 1 << 9,
    Static = 1 << 10,
    Value = 1 << 11,
    ArgumentName = 1 << 12,
    MemberName = 1 << 13,
}

public abstract class Parser_Base
{
    protected ParseNode_Root m_kParseRoot = new ParseNode_Root();
    public ParseNode_Root ParseRoot { get { return m_kParseRoot; } }

    public abstract IdentifierCompletionsType GetCompletionTypes(SyntaxTreeNode_Base afterNode);

    #region Combine Name
    static public ParseNode_Id I(string sName) { return new ParseNode_Id(sName); }

    public void _AR_(ParseNode_Id id, ParseNode_Base rhs, SemanticFlags kSF = 0, bool bContextualKeyword = false, bool bAuto = false)
    {
        m_kParseRoot.AddRule(new ParseNode_Rule(id.Name, rhs) { semantics = kSF, contextualKeyword = bContextualKeyword, autoExclude = bAuto });
    }

    public ParseNode_Alt Alt(ParseNode_Base node)
    {
        return new ParseNode_Alt(node);
    }

    public ParseNode_Alt Alt(params ParseNode_Base[] node)
    {
        return new ParseNode_Alt(node);
    }

    public ParseNode_Seq Seq(params ParseNode_Base[] node)
    {
        return new ParseNode_Seq(node);
    }

    public ParseNode_Many Many(ParseNode_Base node)
    {
        return new ParseNode_Many(node);
    }

    public ParseNode_ManyOpt_If IF(Predicate<SyntaxTreeBuilder> pred, ParseNode_Base node, bool debug = false)
    {
        return new ParseNode_ManyOpt_If(pred, node, debug);
    }

    public ParseNode_ManyOpt_If IF(ParseNode_Base pred, ParseNode_Base node, bool debug = false)
    {
        return new ParseNode_ManyOpt_If(pred, node, debug);
    }

    public ParseNode_ManyOpt_IfNot NIF(ParseNode_Base pred, ParseNode_Base node)
    {
        return new ParseNode_ManyOpt_IfNot(pred, node);
    }

    public ParseNode_Many_Opt Opt(ParseNode_Base node)
    {
        return new ParseNode_Many_Opt(node);
    }

    public ParseNode_Lit Lit(string s)
    {
        return new ParseNode_Lit(s);
    }
    #endregion

    public abstract int TokenToId(string s);

    public virtual string GetToken(int tokenId)
    {
        return ParseRoot.GetToken(tokenId);
    }
}

public static class ParserExtensions
{
    public static ParseNode_Lit ToLit(this string s)
    {
        return new ParseNode_Lit(s);
    }
}
