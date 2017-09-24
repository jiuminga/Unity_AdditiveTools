using System;
using System.Collections.Generic;
using System.Linq;

using Debug = UnityEngine.Debug;

public class Parser_UnityShell : Parser_Base
{
    private static Parser_UnityShell instance;
    public static Parser_UnityShell Instance { get { return instance ?? new Parser_UnityShell(); } }

    public int tokenIdentifier,
        tokenName,
        tokenLiteral,
        tokenAttribute,
        tokenStatement,
        tokenClassBody,
        tokenStructBody,
        tokenInterfaceBody,
        tokenNamespaceBody,
        tokenBinaryOperator,
        tokenExpectedType,
        tokenMemberInitializer,
        tokenNamedParameter,
        tokenEOF;

    public override int TokenToId(string tokenText)
    {
        int id = m_kParseRoot.TokenToId(tokenText);
        if (id < 0)
            id = tokenIdentifier; // parser.TokenToId("IDENTIFIER");
        //Debug.Log("TokenToId(\"" + tokenText + "\") => " + id);
        return id;
    }

    void InitializeTokenCategories()
    {
        tokenIdentifier = TokenToId("IDENTIFIER");
        tokenName = TokenToId("NAME");
        tokenLiteral = TokenToId("LITERAL");
        tokenAttribute = TokenToId(".ATTRIBUTE");
        tokenStatement = TokenToId(".STATEMENT");
        tokenClassBody = TokenToId(".CLASSBODY");
        tokenStructBody = TokenToId(".STRUCTBODY");
        tokenInterfaceBody = TokenToId(".INTERFACEBODY");
        tokenNamespaceBody = TokenToId(".NAMESPACEBODY");
        tokenBinaryOperator = TokenToId(".BINARYOPERATOR");
        tokenExpectedType = TokenToId(".EXPECTEDTYPE");
        tokenMemberInitializer = TokenToId(".MEMBERINITIALIZER");
        tokenNamedParameter = TokenToId(".NAMEDPARAMETER");
        tokenEOF = TokenToId("EOF");
    }

    private Parser_UnityShell()
    {
        instance = this;
        AddRule();
        ParseNode_Rule.debug = false;
        m_kParseRoot.InitSyntax();
        InitializeTokenCategories();
        m_kParseRoot.Init();
    }

    #region ID Define
    public readonly ParseNode_Id EOF = I("EOF");
    public readonly ParseNode_Id IDENTIFIER = I("IDENTIFIER");
    public readonly ParseNode_Id LITERAL = I("LITERAL");
    public readonly ParseNode_Id NAME = new ParseNode_Id_Name();

    //Using
    public readonly ParseNode_Id Directive_Using = I("usingDirective");
    public readonly ParseNode_Id Directive_UsingAlias = I("Directive_UsingAlias");
    public readonly ParseNode_Id Directive_UsingNamespace = I("Directive_UsingNamespace");
    public readonly ParseNode_Id Name_Namespace = I("namespaceName");
    public readonly ParseNode_Id namespaceOrTypeName = I("namespaceOrTypeName");
    public readonly ParseNode_Id Declaration_localVariable = I("localVariableDeclaration");
    public readonly ParseNode_Id globalNamespace = I("globalNamespace");
    public readonly ParseNode_Id typeOrGeneric = I("typeOrGeneric");
    public readonly ParseNode_Id Type = I("type");
    public readonly ParseNode_Id typeArgumentList = I("typeArgumentList");
    public readonly ParseNode_Id Type_Predefined = I("predefinedType");
    public readonly ParseNode_Id unboundTypeRank = I("unboundTypeRank");
    public readonly ParseNode_Id rankSpecifiers = I("rankSpecifiers");
    public readonly ParseNode_Id Name_Type = I("typeName");
    public readonly ParseNode_Id localVariableType = I("localVariableType");
    public readonly ParseNode_Id VAR = I("VAR");

    public readonly ParseNode_Id statement = I("statement");

    //LocalVariables
    public readonly ParseNode_Id Declarator_localVariables = I("localVariableDeclarators");
    public readonly ParseNode_Id Declarator_localVariable = I("localVariableDeclarator");
    public readonly ParseNode_Id localVariableInitializer = I("localVariableInitializer");
    public readonly ParseNode_Id Expression = I("expression");
    public readonly ParseNode_Id arrayInitializer = I("arrayInitializer");
    public readonly ParseNode_Id unaryExpression = I("unaryExpression");
    public readonly ParseNode_Id nonAssignmentExpression = I("nonAssignmentExpression");
    public readonly ParseNode_Id variableInitializerList = I("variableInitializerList");
    public readonly ParseNode_Id castExpression = I("castExpression");
    public readonly ParseNode_Id primaryExpression = I("primaryExpression");
    public readonly ParseNode_Id preIncrementExpression = I("preIncrementExpression");
    public readonly ParseNode_Id preDecrementExpression = I("preDecrementExpression");//

    public readonly ParseNode_Id conditionalExpression = I("conditionalExpression");
    public readonly ParseNode_Id Type_2 = I("type2");
    public readonly ParseNode_Id Expression_NullCoalescing = I("nullCoalescingExpression");
    public readonly ParseNode_Id Expression_ConditionalOr = I("conditionalOrExpression");
    public readonly ParseNode_Id Expression_ConditionalAnd = I("conditionalAndExpression");
    public readonly ParseNode_Id Expression_InclusiveOr = I("inclusiveOrExpression");
    public readonly ParseNode_Id Expression_ExclusiveOr = I("exclusiveOrExpression");
    public readonly ParseNode_Id Expression_And = I("andExpression");
    public readonly ParseNode_Id Expression_Equality = I("equalityExpression");
    public readonly ParseNode_Id Expression_Relational = I("relationalExpression");
    public readonly ParseNode_Id Expression_Shift = I("shiftExpression");
    public readonly ParseNode_Id Expression_Additive = I("additiveExpression");
    public readonly ParseNode_Id Expression_Multiplicative = I("multiplicativeExpression");

    public readonly ParseNode_Id variableInitializer = I("variableInitializer");
    public readonly ParseNode_Id primaryExpressionStart = I("primaryExpressionStart");
    public readonly ParseNode_Id primaryExpressionPart = I("primaryExpressionPart");
    public readonly ParseNode_Id Type_NonArray = I("nonArrayType");
    public readonly ParseNode_Id objectCreationExpression = I("objectCreationExpression");
    public readonly ParseNode_Id arrayCreationExpression = I("arrayCreationExpression");

    public readonly ParseNode_Id implicitArrayCreationExpression = I("implicitArrayCreationExpression");
    public readonly ParseNode_Id typeofExpression = I("typeofExpression");
    public readonly ParseNode_Id sizeofExpression = I("sizeofExpression");
    public readonly ParseNode_Id defaultValueExpression = I("defaultValueExpression");
    public readonly ParseNode_Id accessIdentifier = I("accessIdentifier");
    public readonly ParseNode_Id brackets = I("brackets");
    public readonly ParseNode_Id Type_Simple = I("simpleType");

    public readonly ParseNode_Id objectOrCollectionInitializer = I("objectOrCollectionInitializer");
    public readonly ParseNode_Id expressionList = I("expressionList");
    public readonly ParseNode_Id rankSpecifier = I("rankSpecifier");
    public readonly ParseNode_Id objectInitializer = I("objectInitializer");
    public readonly ParseNode_Id collectionInitializer = I("collectionInitializer");
    public readonly ParseNode_Id memberInitializerList = I("memberInitializerList");
    public readonly ParseNode_Id elementInitializerList = I("elementInitializerList");
    public readonly ParseNode_Id memberInitializer = I("memberInitializer");
    public readonly ParseNode_Id elementInitializer = I("elementInitializer");
    public readonly ParseNode_Id Type_Numric = I("numericType");
    public readonly ParseNode_Id Type_Int = I("integralType");
    public readonly ParseNode_Id Type_Float = I("floatingPointType");
    public readonly ParseNode_Id arguments = I("arguments");
    public readonly ParseNode_Id List_Argument = I("argumentList");
    public readonly ParseNode_Id argument = I("argument");
    public readonly ParseNode_Id argumentValue = I("argumentValue");
    public readonly ParseNode_Id argumentName = I("argumentName");
    public readonly ParseNode_Id variableReference = I("variableReference");


    //public readonly ParseNode_Id expressionStatement = I("expressionStatement");
    //public readonly ParseNode_Id Expression_Statement = I("statementExpression");


    public readonly ParseNode_Id UnityShellCommand = I("UnityShellCommand");
    public readonly ParseNode_Id CommandName = I("CommandName");
    public readonly ParseNode_Id CommandArg = I("CommandArg");

    //public readonly ParseNode_Id assignmentOperator = I("assignmentOperator");
    //public readonly ParseNode_Id anonymousMethodExpression = I("anonymousMethodExpression");
    //public readonly ParseNode_Id anonymousObjectCreationExpression = I("anonymousObjectCreationExpression");
    //public readonly ParseNode_Id anonymousObjectInitializer = I("anonymousObjectInitializer");
    //public readonly ParseNode_Id explicitAnonymousFunctionSignature = I("explicitAnonymousFunctionSignature");
    //public readonly ParseNode_Id anonymousMethodBody = I("anonymousMethodBody");
    //public readonly ParseNode_Id Declarator_memberList = I("memberDeclaratorList");
    //public readonly ParseNode_Id explicitAnonymousFunctionParameterList = I("explicitAnonymousFunctionParameterList");

    //public readonly ParseNode_Id assignment = I("assignment");
    //public readonly ParseNode_Id Name_Member = I("memberName");
    //public readonly ParseNode_Id globalNamespace = I("globalNamespace");

    //public readonly ParseNode_Id Expression_Boolean = I("booleanExpression");

    //public readonly ParseNode_Id Type = I("type");
    //public readonly ParseNode_Id Type_ExecptionClass = I("exceptionClassType");
    //public readonly ParseNode_Id Type_Predefined = I("predefinedType");

    //public readonly ParseNode_Id Declarator_constants = I("constantDeclarators");
    //public readonly ParseNode_Id Declarator_constant = I("constantDeclarator");
    //public readonly ParseNode_Id Declarator_variables = I("variableDeclarators");
    //public readonly ParseNode_Id Declarator_variable = I("variableDeclarator");
    //public readonly ParseNode_Id Declarator_events = I("eventDeclarators");
    //public readonly ParseNode_Id Declarator_operator = I("operatorDeclarator");
    //public readonly ParseNode_Id Declarator_conversionOperator = I("conversionOperatorDeclarator");
    //public readonly ParseNode_Id Declarator_member = I("memberDeclarator");
    //public readonly ParseNode_Id Declarator_event = I("eventDeclarator");

    //public readonly ParseNode_Id Attributes = I("attributes");
    //public readonly ParseNode_Id Modifiers = I("modifiers");
    //public readonly ParseNode_Id qualifiedIdentifier = I("qualifiedIdentifier");

    //public readonly ParseNode_Id classBase = I("classBase");
    //public readonly ParseNode_Id typeParameterConstraintsClauses = I("typeParameterConstraintsClauses");
    //public readonly ParseNode_Id typeParameter = I("typeParameter");
    //public readonly ParseNode_Id typeParameterConstraintsClause = I("typeParameterConstraintsClause");
    //public readonly ParseNode_Id attribute = I("attribute");
    //public readonly ParseNode_Id attributeArgumentList = I("attributeArgumentList");
    //public readonly ParseNode_Id constantExpression = I("constantExpression");


    //public readonly ParseNode_Id qid = I("qid");


    //public readonly ParseNode_Id attributeArguments = I("attributeArguments");
    //public readonly ParseNode_Id formalParameterList = I("formalParameterList");

    //public readonly ParseNode_Id methodHeader = I("methodHeader");
    //public readonly ParseNode_Id formalParameter = I("formalParameter");
    //public readonly ParseNode_Id fixedParameter = I("fixedParameter");
    //public readonly ParseNode_Id parameterModifier = I("parameterModifier");
    //public readonly ParseNode_Id defaultArgument = I("defaultArgument");
    //public readonly ParseNode_Id parameterArray = I("parameterArray");
    //public readonly ParseNode_Id typeVariableName = I("typeVariableName");
    //public readonly ParseNode_Id typeParameterConstraintList = I("typeParameterConstraintList");
    //public readonly ParseNode_Id secondaryConstraintList = I("secondaryConstraintList");
    //public readonly ParseNode_Id secondaryConstraint = I("secondaryConstraint");
    //public readonly ParseNode_Id constructorConstraint = I("constructorConstraint");
    //public readonly ParseNode_Id WHERE = I("WHERE");

    //public readonly ParseNode_Id labeledStatement = I("labeledStatement");
    //public readonly ParseNode_Id embeddedStatement = I("embeddedStatement");
    //public readonly ParseNode_Id selectionStatement = I("selectionStatement");
    //public readonly ParseNode_Id iterationStatement = I("iterationStatement");
    //public readonly ParseNode_Id jumpStatement = I("jumpStatement");
    //public readonly ParseNode_Id tryStatement = I("tryStatement");
    //public readonly ParseNode_Id lockStatement = I("lockStatement");
    //public readonly ParseNode_Id usingStatement = I("usingStatement");
    //public readonly ParseNode_Id yieldStatement = I("yieldStatement");
    //public readonly ParseNode_Id breakStatement = I("breakStatement");
    //public readonly ParseNode_Id continueStatement = I("continueStatement");
    //public readonly ParseNode_Id gotoStatement = I("gotoStatement");
    //public readonly ParseNode_Id returnStatement = I("returnStatement");
    //public readonly ParseNode_Id throwStatement = I("throwStatement");
    //public readonly ParseNode_Id checkedStatement = I("checkedStatement");
    //public readonly ParseNode_Id uncheckedStatement = I("uncheckedStatement");
    //public readonly ParseNode_Id Declaration_localConstant = I("localConstantDeclaration");
    //public readonly ParseNode_Id resourceAcquisition = I("resourceAcquisition");

    //public readonly ParseNode_Id YIELD = I("YIELD");

    //public readonly ParseNode_Id ifStatement = I("ifStatement");
    //public readonly ParseNode_Id elseStatement = I("elseStatement");
    //public readonly ParseNode_Id switchStatement = I("switchStatement");
    //public readonly ParseNode_Id switchBlock = I("switchBlock");
    //public readonly ParseNode_Id switchSection = I("switchSection");
    //public readonly ParseNode_Id switchLabel = I("switchLabel");


    //public readonly ParseNode_Id catchClauses = I("catchClauses");
    //public readonly ParseNode_Id finallyClause = I("finallyClause");
    //public readonly ParseNode_Id specificCatchClauses = I("specificCatchClauses");
    //public readonly ParseNode_Id specificCatchClause = I("specificCatchClause");
    //public readonly ParseNode_Id catchExceptionIdentifier = I("catchExceptionIdentifier");
    //public readonly ParseNode_Id generalCatchClause = I("generalCatchClause");

    //public readonly ParseNode_Id whileStatement = I("whileStatement");
    //public readonly ParseNode_Id doStatement = I("doStatement");
    //public readonly ParseNode_Id forStatement = I("forStatement");
    //public readonly ParseNode_Id foreachStatement = I("foreachStatement");
    //public readonly ParseNode_Id forInitializer = I("forInitializer");
    //public readonly ParseNode_Id forCondition = I("forCondition");
    //public readonly ParseNode_Id forIterator = I("forIterator");
    //public readonly ParseNode_Id statementExpressionList = I("statementExpressionList");


    //public readonly ParseNode_Id accessorModifiers = I("accessorModifiers");
    //public readonly ParseNode_Id accessorBody = I("accessorBody");

    //public readonly ParseNode_Id GET = I("GET");
    //public readonly ParseNode_Id SET = I("SET");


    //public readonly ParseNode_Id ADD = I("ADD");
    //public readonly ParseNode_Id REMOVE = I("REMOVE");

    //public readonly ParseNode_Id operatorBody = I("operatorBody");
    //public readonly ParseNode_Id operatorParameter = I("operatorParameter");
    //public readonly ParseNode_Id binaryOperatorPart = I("binaryOperatorPart");
    //public readonly ParseNode_Id unaryOperatorPart = I("unaryOperatorPart");
    //public readonly ParseNode_Id overloadableBinaryOperator = I("overloadableBinaryOperator");
    //public readonly ParseNode_Id overloadableUnaryOperator = I("overloadableUnaryOperator");


    //public readonly ParseNode_Id attributeTargetSpecifier = I("attributeTargetSpecifier");
    //public readonly ParseNode_Id ATTRIBUTETARGET = I("ATTRIBUTETARGET");




    //public readonly ParseNode_Id checkedExpression = I("checkedExpression");
    //public readonly ParseNode_Id uncheckedExpression = I("uncheckedExpression");

    //public readonly ParseNode_Id parenExpression = I("parenExpression");
    //public readonly ParseNode_Id qidStart = I("qidStart");
    //public readonly ParseNode_Id qidPart = I("qidPart");

    //public readonly ParseNode_Id attributeArgument = I("attributeArgument");

    //public readonly ParseNode_Id attributeMemberName = I("attributeMemberName");

    //public readonly ParseNode_Id memberAccessExpression = I("memberAccessExpression");


    //public readonly ParseNode_Id explicitAnonymousFunctionParameter = I("explicitAnonymousFunctionParameter");
    //public readonly ParseNode_Id anonymousFunctionParameterModifier = I("anonymousFunctionParameterModifier");

    //public readonly ParseNode_Id anonymousFunctionSignature = I("anonymousFunctionSignature");
    //public readonly ParseNode_Id lambdaExpression = I("lambdaExpression");
    //public readonly ParseNode_Id queryExpression = I("queryExpression");
    //public readonly ParseNode_Id lambdaExpressionBody = I("lambdaExpressionBody");
    //public readonly ParseNode_Id implicitAnonymousFunctionParameterList = I("implicitAnonymousFunctionParameterList");
    //public readonly ParseNode_Id implicitAnonymousFunctionParameter = I("implicitAnonymousFunctionParameter");

    //public readonly ParseNode_Id FROM = I("FROM");
    //public readonly ParseNode_Id SELECT = I("SELECT");
    //public readonly ParseNode_Id GROUP = I("GROUP");
    //public readonly ParseNode_Id INTO = I("INTO");
    //public readonly ParseNode_Id ORDERBY = I("ORDERBY");
    //public readonly ParseNode_Id JOIN = I("JOIN");
    //public readonly ParseNode_Id LET = I("LET");
    //public readonly ParseNode_Id ON = I("ON");
    //public readonly ParseNode_Id EQUALS = I("EQUALS");
    //public readonly ParseNode_Id BY = I("BY");
    //public readonly ParseNode_Id ASCENDING = I("ASCENDING");
    //public readonly ParseNode_Id DESCENDING = I("DESCENDING");

    //public readonly ParseNode_Id fromClause = I("fromClause");

    //public readonly ParseNode_Id queryBody = I("queryBody");
    //public readonly ParseNode_Id queryBodyClause = I("queryBodyClause");
    //public readonly ParseNode_Id queryContinuation = I("queryContinuation");
    //public readonly ParseNode_Id letClause = I("letClause");
    //public readonly ParseNode_Id whereClause = I("whereClause");
    //public readonly ParseNode_Id joinClause = I("joinClause");
    //public readonly ParseNode_Id orderbyClause = I("orderbyClause");
    //public readonly ParseNode_Id orderingList = I("orderingList");
    //public readonly ParseNode_Id ordering = I("ordering");
    //public readonly ParseNode_Id selectClause = I("selectClause");
    //public readonly ParseNode_Id groupClause = I("groupClause");
    #endregion

    private void AddRule()
    {
        //Start Rule & Init Parse
        _AR_(I("UnityShellStatement"), (Directive_Using | statement | UnityShellCommand) - EOF);

        _AR_(Directive_Using, "using" - (IF(IDENTIFIER - "=", Directive_UsingAlias) | Directive_UsingNamespace) - ";");
        _AR_(Directive_UsingNamespace, Name_Namespace, SemanticFlags.UsingNamespace);
        _AR_(Name_Namespace, namespaceOrTypeName);
        _AR_(Directive_UsingAlias, IDENTIFIER - "=" - namespaceOrTypeName, SemanticFlags.UsingAlias);
        _AR_(namespaceOrTypeName, IF(IDENTIFIER - "::", globalNamespace - "::") - typeOrGeneric - Many("." - typeOrGeneric));
        _AR_(globalNamespace, IDENTIFIER | "global");
        _AR_(typeOrGeneric, IDENTIFIER - IF("<" - Type, typeArgumentList) - IF("<" - (Lit(",") | ">"), unboundTypeRank));
        _AR_(Type, Type_Predefined - Opt("?") - Opt(rankSpecifiers)
            | Name_Type - Opt("?") - Opt(rankSpecifiers));
        _AR_(typeArgumentList, "<" - Type - Many("," - Type) - ">");
        _AR_(Type_Predefined, Lit("bool") | "byte" | "char" | "decimal" | "double" |
            "float" | "int" | "long" | "object" | "sbyte" | "short" | "string" | "uint" | "ulong" | "ushort");
        _AR_(unboundTypeRank, "<" - Many(",") - ">");
        _AR_(rankSpecifiers, "[" - Many(",") - "]" - Many("[" - Many(",") - "]"));
        _AR_(Name_Type, namespaceOrTypeName);
        _AR_(Declaration_localVariable, localVariableType - Declarator_localVariables);
        _AR_(localVariableType, IF(s => s.TokenScanner.Current.text == "var", VAR) | Type);
        _AR_(VAR, IDENTIFIER | "var", 0, true);


        _AR_(Declarator_localVariables, Declarator_localVariable - Many("," - Declarator_localVariable));
        _AR_(Declarator_localVariable, NAME - Opt("=" - localVariableInitializer), SemanticFlags.LocalVariableDeclarator);
        _AR_(localVariableInitializer, Expression | arrayInitializer, SemanticFlags.LocalVariableInitializerScope);
        _AR_(Expression, nonAssignmentExpression);
        _AR_(arrayInitializer, "{" - Opt(variableInitializerList) - "}");

        _AR_(unaryExpression,
            IF("(" - Type - ")" - IDENTIFIER, castExpression)
            | IF(castExpression, castExpression)
            | primaryExpression - Many(Lit("++") | "--") // TODO: Fix this! Post increment operators should be primaryExpressionPart
            | Seq(Lit("+") | "-" | "!", unaryExpression)
            | Seq("~", unaryExpression | ".EXPECTEDTYPE") | preIncrementExpression | preDecrementExpression);
        //_AR_(assignmentOperator, Lit("=") | "+=" | "-=" | "*=" | "/=" | "%=" | "&=" | "|=" | "^=" | "<<=" | Seq(">", ">="));
        //_AR_(assignment, unaryExpression - assignmentOperator - (Expression | ".EXPECTEDTYPE"));
        _AR_(nonAssignmentExpression,
            //IF(anonymousFunctionSignature - "=>", lambdaExpression)
            //| IF(IDENTIFIER - IDENTIFIER - "in", queryExpression)
            //| IF(IDENTIFIER - Type - IDENTIFIER - "in", queryExpression)
            conditionalExpression);
        _AR_(variableInitializerList, variableInitializer - Many(NIF(Seq(",", "}"), "," - Opt(variableInitializer))) - Opt(","));
        _AR_(primaryExpression,
            primaryExpressionStart - Many(primaryExpressionPart)
            | Seq("new",
                ((Type_NonArray | ".EXPECTEDTYPE") - (objectCreationExpression | arrayCreationExpression))
                | implicitArrayCreationExpression /*| anonymousObjectCreationExpression*/, Many(primaryExpressionPart))
            /*| anonymousMethodExpression*/);
        _AR_(preIncrementExpression, "++" - unaryExpression);
        _AR_(preDecrementExpression, "--" - unaryExpression);

        _AR_(conditionalExpression, Expression_NullCoalescing - Opt("?" - Expression - ":" - Expression), 0, false, true);
        _AR_(Expression_NullCoalescing, Expression_ConditionalOr - Many("??" - Expression_ConditionalOr), 0, false, true);
        _AR_(Type_2, Type_Predefined - Opt(rankSpecifiers) | Name_Type - Opt(rankSpecifiers));
        _AR_(Expression_ConditionalOr, Expression_ConditionalAnd - Many("||" - Expression_ConditionalAnd), 0, false, true);
        _AR_(Expression_ConditionalAnd, Expression_InclusiveOr - Many("&&" - Expression_InclusiveOr), 0, false, true);
        _AR_(Expression_InclusiveOr, Expression_ExclusiveOr - Many("|" - (Expression_ExclusiveOr | ".EXPECTEDTYPE")), 0, false, true);
        _AR_(Expression_ExclusiveOr, Expression_And - Many("^" - Expression_And), 0, false, true);
        _AR_(Expression_And, Expression_Equality - Many("&" - (Expression_Equality | ".EXPECTEDTYPE")), 0, false, true);
        _AR_(Expression_Equality, Expression_Relational - Many((Lit("==") | "!=") - (Expression_Relational | ".EXPECTEDTYPE")), 0, false, true);
        _AR_(Expression_Relational,
            Seq(Expression_Shift, Many(
                Seq(Lit("<") | ">" | "<=" | ">=", (Expression_Shift | ".EXPECTEDTYPE"))
                | Seq(Lit("is") | "as", IF(Type_2 - "?" - Expression - ":", Type_2) | Type)))
            , 0, false, true);
        _AR_(Expression_Shift, Seq(Expression_Additive, Many(IF(Seq(">", ">") | "<<", Seq(Seq(">", ">") | "<<", Expression_Additive)))), 0, false, true);
        _AR_(Expression_Additive, Seq(Expression_Multiplicative, Many(Seq(Lit("+") | "-", Expression_Multiplicative))), 0, false, true);
        _AR_(Expression_Multiplicative, Seq(unaryExpression, Many(Seq(Lit("*") | "/" | "%", unaryExpression))), 0, false, true);

        _AR_(variableInitializer, Expression | arrayInitializer | ".EXPECTEDTYPE");
        _AR_(primaryExpressionStart,
            Type_Predefined
            | LITERAL | "true" | "false"
            | IF(IDENTIFIER - typeArgumentList /*- (new Lit("(") | ")" | ":" | ";" | "," | "." | "?" | "==" | "!="*/, IDENTIFIER - typeArgumentList)
            | IF(IDENTIFIER - "::", globalNamespace - "::") - IDENTIFIER
            //| parenExpression | "this" | "base"
            | typeofExpression | sizeofExpression /*| checkedExpression | uncheckedExpression*/ | defaultValueExpression);
        _AR_(primaryExpressionPart, accessIdentifier | brackets | arguments);
        _AR_(Type_NonArray, (Name_Type | Type_Simple) - Opt("?") | "object" | "string");
        _AR_(objectCreationExpression, arguments - Opt(objectOrCollectionInitializer) | objectOrCollectionInitializer);
        _AR_(arrayCreationExpression,
            IF(Seq("[", Lit(",") | "]"), rankSpecifiers - arrayInitializer)
            | "[" - expressionList - "]" - Opt(rankSpecifiers) - Opt(arrayInitializer));
        _AR_(implicitArrayCreationExpression, rankSpecifier - arrayInitializer);
        //_AR_(anonymousObjectCreationExpression, anonymousObjectInitializer, SemanticFlags.AnonymousObjectCreation);
        //_AR_(anonymousMethodExpression,
        //    "delegate" - Opt(explicitAnonymousFunctionSignature) - anonymousMethodBody
        //, SemanticFlags.AnonymousMethodDeclaration | SemanticFlags.AnonymousMethodScope);

        _AR_(typeofExpression, Lit("typeof") - "(" - (Type | "void") - ")");
        _AR_(sizeofExpression, Lit("sizeof") - "(" - Type_Simple - ")");
        _AR_(defaultValueExpression, Seq("default", "(", Type, ")"));
        _AR_(accessIdentifier, "." - IDENTIFIER - IF(typeArgumentList, typeArgumentList));
        _AR_(brackets, "[" - Opt(expressionList) - "]");
        _AR_(Type_Simple, Type_Numric | "bool");
        _AR_(objectOrCollectionInitializer, "{" - ((IF(IDENTIFIER - "=", objectInitializer) | "}" | collectionInitializer)));
        _AR_(expressionList, Expression - Many("," - Expression));
        _AR_(rankSpecifier, "[" - Many(",") - "]");
        //_AR_(anonymousObjectInitializer, "{" - Opt(Declarator_memberList) - Opt(",") - "}");
        //_AR_(explicitAnonymousFunctionSignature, "(" - Opt(explicitAnonymousFunctionParameterList) - ")", SemanticFlags.FormalParameterListScope);
        //_AR_(anonymousMethodBody, "{" - statementList - "}", SemanticFlags.AnonymousMethodBodyScope);
        _AR_(objectInitializer, Opt(memberInitializerList) - Opt(",") - (Lit("}") | ".MEMBERINITIALIZER"));
        _AR_(collectionInitializer, elementInitializerList - Opt(",") - "}");
        _AR_(memberInitializerList,
            memberInitializer - Many(NIF(Opt(",") - "}", "," - memberInitializer))
        , SemanticFlags.MemberInitializerScope);
        _AR_(elementInitializerList, elementInitializer - Many(NIF(Opt(",") - "}", "," - elementInitializer)));
        _AR_(memberInitializer, (IDENTIFIER | ".MEMBERINITIALIZER") - "=" - (Expression | objectOrCollectionInitializer | ".EXPECTEDTYPE"));
        _AR_(elementInitializer, nonAssignmentExpression | "{" - expressionList - "}" | ".EXPECTEDTYPE");

        _AR_(arguments, "(" - List_Argument - ")");
        _AR_(Type_Numric, Type_Int | Type_Float | "decimal");
        _AR_(Type_Int, Lit("sbyte") | "byte" | "short" | "ushort" | "int" | "uint" | "long" | "ulong" | "char");
        _AR_(Type_Float, Lit("float") | "double");
        _AR_(List_Argument, Opt(argument - Many("," - argument)), SemanticFlags.ArgumentListScope);
        _AR_(argument, IF(IDENTIFIER - ":", argumentName - argumentValue) | argumentValue);
        _AR_(argumentValue, Expression | Seq(Lit("out") | "ref", variableReference) | ".EXPECTEDTYPE");
        _AR_(argumentName, IDENTIFIER - ":");
        _AR_(variableReference, Expression);


        //_AR_(expressionStatement, Expression_Statement - ";");
        //_AR_(Expression_Statement, Expression);

        _AR_(statement, IF((Type | "var") - IDENTIFIER - (Lit(";") | "=" | "[" | ","), Declaration_localVariable - ";"));
        //| IF(IDENTIFIER - ":", labeledStatement) | Declaration_localConstant | embeddedStatement);

        _AR_(UnityShellCommand, CommandName - Many(CommandArg));
        var lsCommand = new List<string>(AT_Shell.m_dicCommandHandler.Keys);
        ParseNode_Base c = new ParseNode_Lit("");
        foreach (var command in lsCommand) { c = c | command; }
        _AR_(CommandName, c);
        _AR_(CommandArg, Opt(Lit("-")) - IDENTIFIER - Lit(":") - IDENTIFIER);


        //_AR_(attributeArguments, "(" - attributeArgumentList - ")");
        //_AR_(attributeArgumentList, Opt(attributeArgument - Many("," - attributeArgument)), SemanticFlags.AttributeArgumentsScope);
        //_AR_(attributeArgument,
        //    IF(IDENTIFIER - "=", attributeMemberName - argumentValue)
        //    | IF(IDENTIFIER - ":", argumentName - argumentValue) | argumentValue);
        //_AR_(attributeMemberName, IDENTIFIER - "=");

        //_AR_(Expression_Boolean, Expression);

        //_AR_(typeParameter, NAME, SemanticFlags.TypeParameterDeclaration);
        //_AR_(qualifiedIdentifier, NAME - Many(Lit(".") - NAME));


        //_AR_(Name_Member, qid);
        //_AR_(Declarator_constants, Declarator_constant - Many("," - Declarator_constant));
        //_AR_(Declarator_constant, NAME - "=" - constantExpression, SemanticFlags.ConstantDeclarator);
        //_AR_(constantExpression, Expression | ".EXPECTEDTYPE");

        //_AR_(methodHeader, Name_Member - "(" - Opt(formalParameterList) - ")" - Opt(typeParameterConstraintsClauses));
        //_AR_(typeParameterConstraintsClauses, typeParameterConstraintsClause - Many(typeParameterConstraintsClause));//,SemanticFlags.TypeParameterConstraintsScope );
        //_AR_(WHERE, IF(s => s.TokenScanner.Current.text == "where", IDENTIFIER) | "where", 0, true);
        //_AR_(typeParameterConstraintsClause, WHERE - typeVariableName - ":" - typeParameterConstraintList);//,SemanticFlags.TypeParameterConstraint );
        //_AR_(typeParameterConstraintList, (Lit("class") | "struct") - Opt("," - secondaryConstraintList) | secondaryConstraintList);

        //_AR_(secondaryConstraintList, secondaryConstraint - Opt("," - I("secondaryConstraintList")) | constructorConstraint);
        //_AR_(secondaryConstraint, Name_Type);
        //_AR_(typeVariableName, IDENTIFIER);
        //_AR_(constructorConstraint, Lit("new") - "(" - ")");


        //_AR_(Declaration_localConstant, "const" - Type - Declarator_constants - ";");
        //_AR_(labeledStatement, IDENTIFIER - ":" - statement, SemanticFlags.LabeledStatement);
        //_AR_(YIELD, IDENTIFIER | "yield", 0, true);

        //_AR_(lockStatement, Seq("lock", "(", Expression, ")", embeddedStatement));
        //_AR_(usingStatement, Seq("using", "(", resourceAcquisition, ")", embeddedStatement), SemanticFlags.UsingStatementScope);
        //_AR_(resourceAcquisition, IF(localVariableType - IDENTIFIER, Declaration_localVariable) | Expression);
        //_AR_(yieldStatement, YIELD - (("return" - Expression - ";") | (Lit("break") - ";")));

        //_AR_(selectionStatement, ifStatement | switchStatement);
        //_AR_(ifStatement, Lit("if") - "(" - Expression_Boolean - ")" - embeddedStatement - Opt(elseStatement));
        //_AR_(elseStatement, "else" - embeddedStatement);
        //_AR_(switchStatement, Lit("switch") - "(" - Expression - ")" - switchBlock);
        //_AR_(switchBlock, "{" - Many(switchSection) - "}", SemanticFlags.SwitchBlockScope);
        //_AR_(switchLabel, "case" - constantExpression - ":" | Seq("default", ":"));
        //_AR_(jumpStatement, breakStatement | continueStatement | gotoStatement | returnStatement | throwStatement);
        //_AR_(breakStatement, Seq("break", ";"));
        //_AR_(continueStatement, Seq("continue", ";"));
        //_AR_(gotoStatement, "goto" - (IDENTIFIER | "case" - constantExpression | "default") - ";");
        //_AR_(returnStatement, "return" - Opt(Expression) - ";");
        //_AR_(throwStatement, "throw" - Opt(Expression) - ";");

        //_AR_(catchClauses, IF(Seq("catch", "("),
        //    specificCatchClauses - Opt(generalCatchClause)) | generalCatchClause);
        //_AR_(specificCatchClauses, specificCatchClause - Many(IF(Seq("catch", "("), specificCatchClause)));
        //_AR_(formalParameterList, formalParameter - Many("," - formalParameter), SemanticFlags.FormalParameterListScope);
        //_AR_(formalParameter, Attributes - (fixedParameter | parameterArray));
        //_AR_(fixedParameter,
        //    Opt(parameterModifier) - Type - NAME - Opt(defaultArgument)
        //, SemanticFlags.FixedParameterDeclaration);
        //_AR_(parameterModifier, Lit("ref") | "out" | "this");
        //_AR_(defaultArgument, "=" - (Expression | ".EXPECTEDTYPE"));
        //_AR_(parameterArray, "params" - Type - NAME, SemanticFlags.ParameterArrayDeclaration);

        //_AR_(iterationStatement, whileStatement | doStatement | forStatement | foreachStatement);
        //_AR_(whileStatement, Seq("while", "(", Expression_Boolean, ")", embeddedStatement));
        //_AR_(doStatement, "do" - embeddedStatement - "while" - "(" - Expression_Boolean - ")" - ";");
        //_AR_(forStatement,
        //    Seq("for", "(", Opt(forInitializer), ";", Opt(Expression_Boolean), ";",
        //        Opt(forIterator), ")", embeddedStatement), SemanticFlags.ForStatementScope);
        //_AR_(forInitializer, IF(localVariableType - IDENTIFIER, Declaration_localVariable) | statementExpressionList);
        //_AR_(forIterator, statementExpressionList);
        //_AR_(foreachStatement, Lit("foreach") - "(" - localVariableType - NAME - "in" - Expression - ")" - embeddedStatement
        //, SemanticFlags.ForStatementScope | SemanticFlags.ForEachVariableDeclaration);
        //_AR_(statementExpressionList, Seq(Expression_Statement, Many(Seq(",", Expression_Statement))));
        //// TODO: should be assignment, call, increment, decrement, and new object expressions

        //_AR_(accessorModifiers, "internal" - Opt("protected") | "protected" - Opt("internal") | "public" | "private");

        //_AR_(GET, IDENTIFIER | "get", 0, true);
        //_AR_(SET, IDENTIFIER | "set", 0, true);

        //_AR_(Declarator_events, Declarator_event - Many("," - Declarator_event));
        //_AR_(Declarator_event, NAME - Opt("=" - variableInitializer), SemanticFlags.EventDeclarator);

        //_AR_(ADD, IDENTIFIER | "add", 0, true);
        //_AR_(REMOVE, IDENTIFIER | "remove", 0, true);
        //_AR_(Declarator_variables, Seq(Declarator_variable, Many(Seq(",", Declarator_variable))));
        //_AR_(Declarator_variable, NAME - Opt("=" - variableInitializer), SemanticFlags.VariableDeclarator);
        //_AR_(Modifiers, Many(Lit("new") | "public" | "protected" | "internal" | "private" | "abstract" | "sealed" | "static" | "readonly" | "volatile" | "virtual" | "override" | "extern"));

        //_AR_(Declarator_operator, Seq("operator",
        //    Seq(Lit("+") | "-", "(", operatorParameter, binaryOperatorPart | unaryOperatorPart)
        //        | overloadableUnaryOperator - "(" - operatorParameter - unaryOperatorPart
        //        | overloadableBinaryOperator - "(" - operatorParameter - binaryOperatorPart), SemanticFlags.OperatorDeclarator);
        //_AR_(operatorParameter, Type - NAME, SemanticFlags.FixedParameterDeclaration);
        //_AR_(unaryOperatorPart, ")");
        //_AR_(binaryOperatorPart, "," - operatorParameter - ")");
        //_AR_(overloadableUnaryOperator, Lit("!") | "~" | "++" | "--" | "true" | "false");
        //_AR_(overloadableBinaryOperator,
        //    Lit("*") | "/" | "%" | "&" | "|" | "^" | "<<" | IF(Seq(">", ">"),
        //    Seq(">", ">")) | "==" | "!=" | ">" | "<" | ">=" | "<=");
        //_AR_(Declarator_conversionOperator, (Lit("implicit") | "explicit") - "operator" - Type - "(" - operatorParameter - ")", SemanticFlags.ConversionOperatorDeclarator);

        //_AR_(ATTRIBUTETARGET, (IDENTIFIER | "event:" | "return:" | "field:" | "method:" | "param:" | "property:" | "type:" | "assembly:" | "module:"), 0, true);
        //_AR_(attributeTargetSpecifier,
        //    (IF(s => s.TokenScanner.Current.text == "field"
        //        || s.TokenScanner.Current.text == "method" || s.TokenScanner.Current.text == "param" || s.TokenScanner.Current.text == "property"
        //        || s.TokenScanner.Current.text == "type" || s.TokenScanner.Current.text == "assembly" || s.TokenScanner.Current.text == "module",
        //            ATTRIBUTETARGET) | "event" | "return") - ":");
        //_AR_(Attributes, Many(
        //        "[" - IF(attributeTargetSpecifier, attributeTargetSpecifier) - attribute -
        //            Many("," - attribute) - "]"), SemanticFlags.AttributesScope);
        //_AR_(attribute, Name_Type - Opt(attributeArguments) | ".ATTRIBUTE");


        //_AR_(Type_ExecptionClass, Name_Type | "object" | "string");

        //_AR_(globalNamespace, IDENTIFIER | "global");
        //_AR_(namespaceOrTypeName, IF(IDENTIFIER - "::", globalNamespace - "::") - typeOrGeneric - Many("." - typeOrGeneric));
        //_AR_(typeArgumentList, "<" - Type - Many("," - Type) - ">");



        //_AR_(castExpression, "(" - Type - ")" - unaryExpression);




        //_AR_(qid, qidStart - Many(qidPart));
        //_AR_(qidPart, IF("." - IDENTIFIER, accessIdentifier) | ".");


        //_AR_(parenExpression, "(" - Expression - ")");


        //_AR_(Declarator_memberList, Declarator_member - Many(IF("," - IDENTIFIER, "," - Declarator_member)));
        //_AR_(Declarator_member,
        //    IF(IDENTIFIER - "=", IDENTIFIER - "=" - Expression)
        //    | memberAccessExpression, SemanticFlags.MemberDeclarator);
        //_AR_(memberAccessExpression, Expression);




        //_AR_(checkedExpression, Lit("checked") - "(" - Expression - ")");
        //_AR_(uncheckedExpression, Lit("unchecked") - "(" - Expression - ")");

        //#region LambdaLinq

        //_AR_(lambdaExpression,
        //    anonymousFunctionSignature - "=>" - lambdaExpressionBody
        //, SemanticFlags.LambdaExpressionScope | SemanticFlags.LambdaExpressionDeclaration);
        //_AR_(anonymousFunctionSignature,
        //    Seq(
        //        "(",
        //        Opt(
        //            IF(Seq(IDENTIFIER, Lit(",") | ")"), implicitAnonymousFunctionParameterList)
        //            | explicitAnonymousFunctionParameterList),
        //        ")")
        //    | implicitAnonymousFunctionParameter);// ,SemanticFlags.LambdaExpressionDeclaration );
        //_AR_(implicitAnonymousFunctionParameterList,
        //    implicitAnonymousFunctionParameter - Many("," - implicitAnonymousFunctionParameter));
        //_AR_(implicitAnonymousFunctionParameter, IDENTIFIER, SemanticFlags.ImplicitParameterDeclaration);

        //_AR_(explicitAnonymousFunctionParameterList, explicitAnonymousFunctionParameter - Many("," - explicitAnonymousFunctionParameter));
        //_AR_(explicitAnonymousFunctionParameter, Opt(anonymousFunctionParameterModifier) - Type - NAME, SemanticFlags.ExplicitParameterDeclaration);
        //_AR_(anonymousFunctionParameterModifier, Lit("ref") | "out");
        //#endregion

        //#region Linq
        //_AR_(FROM, IDENTIFIER | "from", 0, true);
        //_AR_(SELECT, IDENTIFIER, 0, true);
        //_AR_(GROUP, IDENTIFIER | "group", 0, true);
        //_AR_(INTO, IF(s => s.TokenScanner.Current.text == "into", IDENTIFIER | "into"), 0, true);
        //_AR_(ORDERBY, IF(s => s.TokenScanner.Current.text == "orderby", IDENTIFIER | "orderby"), 0, true);
        //_AR_(JOIN, IF(s => s.TokenScanner.Current.text == "join", IDENTIFIER | "join"), 0, true);
        //_AR_(LET, IF(s => s.TokenScanner.Current.text == "let", IDENTIFIER | "let"), 0, true);
        //_AR_(ON, IF(s => s.TokenScanner.Current.text == "on", IDENTIFIER | "on"), 0, true);
        //_AR_(EQUALS, IF(s => s.TokenScanner.Current.text == "equals", IDENTIFIER | "equals"), 0, true);
        //_AR_(BY, IF(s => s.TokenScanner.Current.text == "by", IDENTIFIER | "by"), 0, true);
        //_AR_(ASCENDING, IDENTIFIER | "ascending", 0, true);
        //_AR_(DESCENDING, IDENTIFIER | "descending", 0, true);

        //_AR_(queryExpression, fromClause - queryBody, SemanticFlags.QueryExpressionScope);
        //_AR_(fromClause,
        //    FROM - (IF(NAME - "in", NAME) | Type - NAME) - "in" - Expression
        //, SemanticFlags.FromClauseVariableDeclaration);
        //_AR_(queryBody,
        //    Many(
        //        IF(s => s.TokenScanner.Current.text == "from"
        //            | s.TokenScanner.Current.text == "let"
        //            | s.TokenScanner.Current.text == "join"
        //            | s.TokenScanner.Current.text == "orderby"
        //            | s.TokenScanner.Current.text == "where", queryBodyClause)
        //    ) - (IF(s => s.TokenScanner.Current.text == "select", selectClause) | "select" | groupClause) - Opt(queryContinuation)
        //, SemanticFlags.QueryBodyScope);
        //_AR_(queryContinuation, INTO - IDENTIFIER - queryBody);
        //_AR_(queryBodyClause,
        //    IF(s => s.TokenScanner.Current.text == "from", fromClause)
        //    | IF(s => s.TokenScanner.Current.text == "let", letClause)
        //    | IF(s => s.TokenScanner.Current.text == "join", joinClause)
        //    | IF(s => s.TokenScanner.Current.text == "orderby", orderbyClause)
        //    | whereClause
        //    );
        //_AR_(joinClause, JOIN - Opt(Type) - IDENTIFIER - "in" - Expression - ON - Expression - EQUALS - Expression - Opt(INTO - IDENTIFIER));
        //_AR_(letClause, LET - IDENTIFIER - "=" - Expression);
        //_AR_(orderbyClause, ORDERBY - orderingList);
        //_AR_(orderingList, ordering - Many("," - ordering));
        //_AR_(ordering, Expression - Opt(IF(s => s.TokenScanner.Current.text == "ascending", ASCENDING) | DESCENDING));
        //_AR_(selectClause, SELECT - Expression);
        //_AR_(groupClause, GROUP - Expression - BY - Expression);
        //_AR_(whereClause, WHERE - Expression_Boolean);
        //#endregion

        //#region booleanExpression

        //#endregion
    }

    public override IdentifierCompletionsType GetCompletionTypes(SyntaxTreeNode_Base afterNode)
    {
        var cit = IdentifierCompletionsType.Namespace | IdentifierCompletionsType.TypeName | IdentifierCompletionsType.Value;
        var leaf = afterNode as SyntaxTreeNode_Leaf;
        if (leaf == null)
            return cit;

        if (leaf.token.text == ".")
            cit = IdentifierCompletionsType.Member | IdentifierCompletionsType.TypeName | IdentifierCompletionsType.Value;
        else if (leaf.token.text == "::")
            cit = IdentifierCompletionsType.Member | IdentifierCompletionsType.TypeName | IdentifierCompletionsType.Namespace;

        var childIndex = afterNode.m_iChildIndex;
        for (var node = afterNode.Parent; node != null; childIndex = node.m_iChildIndex, node = node.Parent)
        {
            switch (node.RuleName)
            {
                case "namespaceName":
                    cit &= ~(IdentifierCompletionsType.TypeName | IdentifierCompletionsType.Value);
                    break;

                case "exceptionClassType":
                    goto breakLoop;

                case "attributes":
                case "attribute":
                    cit |= IdentifierCompletionsType.AttributeClassType;
                    goto breakLoop;

                case "arguments":
                case "attributeArguments":
                    goto breakLoop;

                case "argumentName":
                    cit = IdentifierCompletionsType.ArgumentName;
                    goto breakLoop;

                case "typeName":
                    cit &= ~IdentifierCompletionsType.Value;
                    break;

                case "namespaceOrTypeName":
                    cit &= ~IdentifierCompletionsType.Value;
                    break;

                case "Directive_UsingAlias":
                    break;

                case "statement":
                    if (childIndex == 0)
                        cit |= IdentifierCompletionsType.Namespace | IdentifierCompletionsType.TypeName | IdentifierCompletionsType.Value;
                    break;

                case "accessIdentifier":
                    cit &= ~IdentifierCompletionsType.Namespace;
                    goto breakLoop;
            }
        }
        breakLoop:
        return cit;
    }

    public static SyntaxTreeNode_Rule EnclosingSemanticNode(SyntaxTreeNode_Base node, SemanticFlags flags)
    {
        if (node is SyntaxTreeNode_Leaf)
            return EnclosingSemanticNode(node.Parent, flags);
        return EnclosingSemanticNode((SyntaxTreeNode_Rule)node, flags);
    }

    public static SyntaxTreeNode_Rule EnclosingSemanticNode(SyntaxTreeNode_Rule node, SemanticFlags flags)
    {
        while (node != null)
        {
            var scopeSemantics = node.Semantics & flags;
            if (scopeSemantics != SemanticFlags.None)
                return node;
            node = node.Parent;
        }
        return null;
    }

    public static SyntaxTreeNode_Rule EnclosingScopeNode(SyntaxTreeNode_Rule node)
    {
        while (node != null)
        {
            var scopeSemantics = node.Semantics & SemanticFlags.ScopesMask;
            if (scopeSemantics != SemanticFlags.None)
                return node;
            node = node.Parent;
        }
        return null;
    }

    public static SyntaxTreeNode_Rule EnclosingScopeNode(SyntaxTreeNode_Rule node, params SemanticFlags[] scopeTypes)
    {
        while (node != null)
        {
            var scopeSemantics = node.Semantics & SemanticFlags.ScopesMask;
            if (scopeSemantics != SemanticFlags.None)
                if (scopeTypes.Contains(scopeSemantics))
                    return node;
            node = node.Parent;
        }
        return null;
    }

    private static Scope_Base GetNodeScope(SyntaxTreeNode_Rule node, SyntaxTreeBuilder stb)
    {
        if (node == null)
            return null;

        var nodeScopeAsSDS = node.scope as Scope_SymbolDeclaration;
        if (node.scope == null || nodeScopeAsSDS != null && nodeScopeAsSDS.declaration == null)
        {
            var enclosingScopeNode = EnclosingSemanticNode(node.Parent, SemanticFlags.ScopesMask);
            var enclosingScope = GetNodeScope(enclosingScopeNode, stb);

            var scopeSemantics = node.Semantics & SemanticFlags.ScopesMask;
            switch (scopeSemantics)
            {
                case SemanticFlags.CompilationUnitScope:
                    var scope = stb.ComplilationUnitScop;
                    if (scope == null)
                        return null;
                    scope.declaration.parseTreeNode = node;
                    node.scope = scope;
                    break;

                case SemanticFlags.NamespaceBodyScope:
                    var declaration = GetNodeDeclaration(node.Parent);
                    node.scope = new Scope_Namespace(node)
                    {
                        declaration = (Declaration_Namespace)declaration,
                        definition = (SD_NameSpace)declaration.definition,
                        parentScope = enclosingScope,
                    };
                    break;

                case SemanticFlags.ClassBaseScope:
                case SemanticFlags.StructInterfacesScope:
                case SemanticFlags.InterfaceBaseScope:
                    declaration = (enclosingScope as Scope_SymbolDeclaration).declaration;
                    node.scope = new Scope_TypeBase(node) { parentScope = enclosingScope, definition = declaration != null ? declaration.definition as TypeDefinitionBase : null };
                    break;
                case SemanticFlags.EnumBaseScope:
                    node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.TypeParameterConstraintsScope:
                    break;
                case SemanticFlags.ClassBodyScope:
                case SemanticFlags.StructBodyScope:
                case SemanticFlags.InterfaceBodyScope:
                case SemanticFlags.EnumBodyScope:
                    declaration = GetNodeDeclaration(node.Parent);
                    if (declaration == null)
                        node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    else
                        node.scope = new Scope_Body(node) { parentScope = enclosingScope, definition = declaration.definition };
                    break;
                case SemanticFlags.AnonymousMethodScope:
                case SemanticFlags.LambdaExpressionScope:
                    declaration = GetNodeDeclaration(node);
                    node.scope = new Scope_SymbolDeclaration(node) { parentScope = enclosingScope, declaration = declaration };
                    break;
                case SemanticFlags.AnonymousMethodBodyScope:
                case SemanticFlags.LambdaExpressionBodyScope:
                    declaration = GetNodeDeclaration(node.Parent);
                    node.scope = new Scope_Body(node) { parentScope = enclosingScope, definition = declaration.definition };
                    break;
                case SemanticFlags.MethodBodyScope:
                    if ((node.Parent.Semantics & SemanticFlags.SymbolDeclarationsMask) != SemanticFlags.None)
                        declaration = GetNodeDeclaration(node.Parent);
                    else
                        declaration = node.Parent.NodeAt(0) == null ? null : GetNodeDeclaration(node.Parent.NodeAt(0));
                    node.scope = declaration == null ? null :
                        new Scope_Body(node) { parentScope = enclosingScope, definition = declaration.definition };
                    break;
                case SemanticFlags.AccessorBodyScope:
                    declaration = GetNodeDeclaration(node.Parent);
                    node.scope = new Scope_AccessorBody(node)
                    {
                        parentScope = enclosingScope,
                        definition = declaration != null ? declaration.definition : null
                    };
                    break;
                case SemanticFlags.CodeBlockScope:
                case SemanticFlags.QueryExpressionScope:
                case SemanticFlags.QueryBodyScope:
                    node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.FormalParameterListScope:
                    declaration =
                        (node.Parent.Semantics & SemanticFlags.SymbolDeclarationsMask) != SemanticFlags.None
                        ? GetNodeDeclaration(node.Parent)
                        : GetNodeDeclaration(node.Parent.Parent);
                    if (declaration != null)
                    {
                        node.scope = new Scope_Body(node) { parentScope = enclosingScope, definition = declaration.definition };
                    }
                    break;
                case SemanticFlags.ConstructorInitializerScope:
                    break;
                case SemanticFlags.SwitchBlockScope:
                case SemanticFlags.ForStatementScope:
                case SemanticFlags.UsingStatementScope:
                    node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.EmbeddedStatementScope:
                    break;
                case SemanticFlags.LocalVariableInitializerScope:
                    break;
                case SemanticFlags.SpecificCatchScope:
                    node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.ArgumentListScope:
                    node.scope = new Scope_Local(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.AttributeArgumentsScope:
                    node.scope = new Scope_AttributeArguments(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.MemberInitializerScope:
                    node.scope = new Scope_MemberInitializer(node) { parentScope = enclosingScope };
                    break;
                case SemanticFlags.TypeDeclarationScope:
                    declaration = GetNodeDeclaration(node);
                    node.scope = new Scope_SymbolDeclaration(node) { parentScope = enclosingScope, declaration = declaration };
                    break;
                case SemanticFlags.MemberDeclarationScope:
                    break;
                case SemanticFlags.MethodDeclarationScope:
                    declaration = GetNodeDeclaration(node);
                    node.scope = new Scope_SymbolDeclaration(node) { parentScope = enclosingScope, declaration = declaration };
                    break;
                case SemanticFlags.AccessorsListScope:
                    declaration = GetNodeDeclaration(node);
                    node.scope = new Scope_SymbolDeclaration(node) { parentScope = enclosingScope, declaration = declaration };
                    break;
                case SemanticFlags.AttributesScope:
                    node.scope = new Scope_Attributes(node) { parentScope = enclosingScope };
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unhandled case " + scopeSemantics + ": in switch statement!\nsemantics: " + node.Semantics);
            }
            if (node.scope == null)
                node.scope = new Scope_Local(node) { parentScope = enclosingScope };
        }
        return node.scope;
    }

    private static SymbolDeclaration GetNodeDeclaration(SyntaxTreeNode_Rule node, SyntaxTreeBuilder stb = null)
    {
        if (node.declaration != null) return node.declaration;

        var enclosingScopeNode = EnclosingSemanticNode(node.Parent, SemanticFlags.ScopesMask);
        var enclosingScope = GetNodeScope(enclosingScopeNode, stb);

        if (enclosingScope == null)
            return null;

        SyntaxTreeNode_Base modifiersNode = null;
        SyntaxTreeNode_Base partialNode = null;
        SyntaxTreeNode_Rule typeParamsNode = null;

        var declarationSemantics = node.Semantics & SemanticFlags.SymbolDeclarationsMask;
        switch (declarationSemantics)
        {
            case SemanticFlags.Declaration_Namespace:
                node.declaration = new Declaration_Namespace { parseTreeNode = node, kind = SymbolKind.Namespace };
                break;

            case SemanticFlags.UsingNamespace:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.ImportedNamespace };
                break;

            case SemanticFlags.UsingAlias:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.TypeAlias };
                break;

            case SemanticFlags.ExternAlias:
                break;
            case SemanticFlags.ClassDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                partialNode = node.Parent.FindChildByName("PARTIAL");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Class };
                typeParamsNode = node.FindChildByName("typeParameterList") as SyntaxTreeNode_Rule;
                //	Debug.Log(node.declaration + " mods: " + node.declaration.modifiers);
                break;
            case SemanticFlags.TypeParameterDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.TypeParameter };
                break;
            case SemanticFlags.BaseListDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.BaseTypesList };
                break;
            case SemanticFlags.ConstantDeclarator:
                modifiersNode = node.Parent.Parent.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration
                {
                    parseTreeNode = node,
                    kind =
                        node.Parent.Parent.RuleName == "Declaration_Constant" ?
                        SymbolKind.ConstantField :
                        SymbolKind.LocalConstant
                };
                break;
            case SemanticFlags.ConstructorDeclarator:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Constructor };
                break;
            case SemanticFlags.DestructorDeclarator:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Destructor };
                break;
            case SemanticFlags.OperatorDeclarator:
            case SemanticFlags.ConversionOperatorDeclarator:
                modifiersNode = node.Parent.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Method };
                break;
            case SemanticFlags.MethodDeclarator:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                typeParamsNode = node.NodeAt(0).NodeAt(0).NodeAt(0).NodeAt(-1).FindChildByName("typeParameterList") as SyntaxTreeNode_Rule;
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Method };
                break;
            case SemanticFlags.LocalVariableDeclarator:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Variable };
                break;
            case SemanticFlags.ForEachVariableDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.ForEachVariable };
                break;
            case SemanticFlags.FromClauseVariableDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.FromClauseVariable };
                break;
            case SemanticFlags.LabeledStatement:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Label };
                break;
            case SemanticFlags.CatchExceptionParameterDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.CatchParameter };
                break;
            case SemanticFlags.FixedParameterDeclaration:
                modifiersNode = node.FindChildByName("parameterModifier");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Parameter };
                break;
            case SemanticFlags.ParameterArrayDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Parameter, modifiers = global::Modifiers.Params };
                break;
            case SemanticFlags.ImplicitParameterDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Parameter };
                break;
            case SemanticFlags.ExplicitParameterDeclaration:
                modifiersNode = node.FindChildByName("anonymousFunctionParameterModifier");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Parameter };
                break;
            case SemanticFlags.PropertyDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Property };
                break;
            case SemanticFlags.IndexerDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Indexer };
                break;
            case SemanticFlags.GetAccessorDeclaration:
            case SemanticFlags.SetAccessorDeclaration:
                modifiersNode = node.Parent.FindChildByName("accessorModifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Accessor };
                break;
            case SemanticFlags.InterfaceGetAccessorDeclaration:
            case SemanticFlags.InterfaceSetAccessorDeclaration:
            case SemanticFlags.AddAccessorDeclaration:
            case SemanticFlags.RemoveAccessorDeclaration:
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Accessor };
                break;
            case SemanticFlags.EventDeclarator:
                modifiersNode = node.Parent.Parent.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Event };
                break;
            case SemanticFlags.EventWithAccessorsDeclaration:
                modifiersNode = node.Parent.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Event };
                break;
            case SemanticFlags.VariableDeclarator:
                modifiersNode = node.Parent.Parent.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Field };
                break;
            case SemanticFlags.StructDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                partialNode = node.Parent.FindChildByName("PARTIAL");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Struct };
                break;
            case SemanticFlags.InterfaceDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                partialNode = node.Parent.FindChildByName("PARTIAL");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Interface };
                break;
            case SemanticFlags.InterfaceIndexerDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Property };
                break;
            case SemanticFlags.InterfacePropertyDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Property };
                break;
            case SemanticFlags.InterfaceMethodDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Method };
                break;
            case SemanticFlags.InterfaceEventDeclaration:
                break;
            case SemanticFlags.EnumDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Enum };
                break;
            case SemanticFlags.EnumMemberDeclaration:
                node.declaration = new SymbolDeclaration
                {
                    parseTreeNode = node,
                    kind = SymbolKind.EnumMember,
                    modifiers = global::Modifiers.ReadOnly | global::Modifiers.Public | global::Modifiers.Static
                };
                break;
            case SemanticFlags.DelegateDeclaration:
                modifiersNode = node.Parent.FindChildByName("modifiers");
                typeParamsNode = node.FindChildByName("typeParameterList") as SyntaxTreeNode_Rule;
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.Delegate };
                break;
            case SemanticFlags.AnonymousObjectCreation:
                break;
            case SemanticFlags.MemberDeclarator:
                break;
            case SemanticFlags.LambdaExpressionDeclaration:
            case SemanticFlags.AnonymousMethodDeclaration:
                enclosingScopeNode = EnclosingScopeNode(node.Parent,
                    SemanticFlags.CodeBlockScope,
                    SemanticFlags.MethodBodyScope,
                    SemanticFlags.TypeDeclarationScope);
                enclosingScope = GetNodeScope(enclosingScopeNode, stb);
                node.declaration = new SymbolDeclaration { parseTreeNode = node, kind = SymbolKind.LambdaExpression };
                break;
            case SemanticFlags.None:
                Debug.LogWarning("declarationSemantics is None on " + node);
                break;
            default:
                throw new ArgumentOutOfRangeException("Unhandled case " + declarationSemantics + " for node " + node);
        }

        if (node.declaration != null)
        {
            if (modifiersNode != null)
                node.declaration.modifiers = ParseModifiers(modifiersNode);
            if (partialNode != null)
                node.declaration.modifiers |= global::Modifiers.Partial;
            if (typeParamsNode != null)
                node.declaration.numTypeParameters = CountTypeParameters(typeParamsNode);
            if (enclosingScope == null)
                Debug.LogWarning("Symbol declaration " + declarationSemantics + " outside of declaration space!\nenclosingScopeNode: " + (enclosingScopeNode != null ? enclosingScopeNode.RuleName : "null") + "\nnode: " + node);
            else
            {
                var saved = SymbolReference.dontResolveNow;
                SymbolReference.dontResolveNow = true;
                try
                {
                    enclosingScope.AddDeclaration(node.declaration);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    SymbolReference.dontResolveNow = saved;
                }
                ++LR_SyntaxTree.resolverVersion;
                if (LR_SyntaxTree.resolverVersion == 0)
                    ++LR_SyntaxTree.resolverVersion;
            }
        }
        return node.declaration;
    }

    private static int CountTypeParameters(SyntaxTreeNode_Rule typeParamsNode)
    {
        var count = typeParamsNode.NumValidNodes > 0 ? 1 : 0;
        for (var i = 1; i < typeParamsNode.NumValidNodes; ++i)
            if (typeParamsNode.ChildAt(i).IsLit(","))
                ++count;
        return count;
    }

    private static Modifiers ParseModifiers(SyntaxTreeNode_Base node)
    {
        var root = node as SyntaxTreeNode_Rule;
        if (root == null || root.Nodes == null)
            return global::Modifiers.None;
        var mods = global::Modifiers.None;
        foreach (var mod in root.Nodes)
            switch (mod.Print())
            {
                case "public":
                    mods |= global::Modifiers.Public;
                    break;
                case "internal":
                    mods |= global::Modifiers.Internal;
                    break;
                case "protected":
                    mods |= global::Modifiers.Protected;
                    break;
                case "private":
                    mods |= global::Modifiers.Private;
                    break;
                case "static":
                    mods |= global::Modifiers.Static;
                    break;
                case "new":
                    mods |= global::Modifiers.New;
                    break;
                case "sealed":
                    mods |= global::Modifiers.Sealed;
                    break;
                case "abstract":
                    mods |= global::Modifiers.Abstract;
                    break;
                case "readonly":
                    mods |= global::Modifiers.ReadOnly;
                    break;
                case "volatile":
                    mods |= global::Modifiers.Volatile;
                    break;
                case "virtual":
                    mods |= global::Modifiers.Virtual;
                    break;
                case "override":
                    mods |= global::Modifiers.Override;
                    break;
                case "extern":
                    mods |= global::Modifiers.Extern;
                    break;
                case "ref":
                    mods |= global::Modifiers.Ref;
                    break;
                case "out":
                    mods |= global::Modifiers.Out;
                    break;
                case "this":
                    mods |= global::Modifiers.This;
                    break;
                default:
                    return mods; // Cancelling...
            }
        return mods;
    }

    public void OnSemanticNodeClose(SyntaxTreeBuilder stb, SyntaxTreeNode_Rule node)
    {
        if (node.NumValidNodes == 0)
            return;

        if ((node.Semantics & SemanticFlags.SymbolDeclarationsMask) != SemanticFlags.None)
            GetNodeDeclaration(node, stb);
        if ((node.Semantics & SemanticFlags.ScopesMask) != SemanticFlags.None)
            GetNodeScope(node, stb);
    }
}

