using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {

    internal enum ExpressionNodeType : ushort {

        Invalid = 0,
        Literal,
        ParenExpression,
        TypeModifier,
        TypeArgumentList,
        TypePath,
        NewExpression,
        DefaultExpression,
        SizeOfExpression,
        TypeOfExpression,
        NameOfExpression,
        MemberAccess,
        DirectCast,
        AssignmentExpression,
        PrimaryExpression,
        PrefixUnaryExpression,
        Identifier,
        TypeNamePart,
        BracketExpression,
        PrimaryExpressionPart,
        BaseAccessExpression,
        TupleExpression,
        CheckedExpression,
        AnonymousMethodExpression,
        LiteralAccess,
        MemberInitializer,
        ObjectInitializer,
        CollectionInitializer,
        ArrayInitializer,
        ElementInitializer,
        LambdaExpression,
        BlockExpression,
        YieldStatement,
        ReturnStatement,
        UsingStatement,
        LockStatement,
        TryCatchFinally,
        ThrowStatement,
        GoToStatement,
        ContinueStatement,
        BreakStatement,
        ForeachLoop,
        ForLoop,
        WhileLoop,
        SwitchStatement,
        IfStatement,
        Catch,
        RangeExpression,
        BinaryExpression,
        TypePatternArm,
        IsTypeExpression,
        TernaryExpression,
        MethodInvocation,
        IncrementDecrement,
        SwitchLabel,
        SwitchArm,
        SwitchExpression,
        SwitchSection,
        ResolveIdExpression,
        PrimaryIdentifier,

        Argument, // used for accepting an argument to a function 
        VariableDeclaration,
        LocalFunctionDefinition,

        Parameter,

        UnboundTypeNameExpression,

        IsNullExpression,

        BinaryPattern,

        VariableDesignation,

        RelationalPattern,

        TypePattern,

        UnaryNotPattern,

        VarPattern,

        DiscardPattern,

        DeclarationPattern,

        ArrayCreationRank,

        ConstantPattern,

        StringInterpolation,

        StringInterpolationPart

    }

    internal unsafe interface IPrintableExpressionNode {

        void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex);

    }

    internal enum IdentifierType {

        Invalid,
        Keyword,
        DollarIdentifier,
        DollarThis,
        RuntimeIdentifier,
        NormalIdentifier,
        This,
        PredefinedType

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ExpressionNodeHeader {

        public ushort nodeIndexShort;
        public ExpressionNodeType type;
        public NonTrivialTokenRange tokenRange;

        public ExpressionIndex expressionIndex => new ExpressionIndex(nodeIndexShort);

    }

    internal unsafe struct ExpressionNodeData {

        public fixed byte bytes[24]; // largest size right now 

    }

    [DebuggerDisplay("{DebugDisplay()}")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UntypedExpressionNode {

        public ExpressionNodeHeader meta;
        private ExpressionNodeData data;

        public string DebugDisplay() {
            return meta.type.ToString();
        }

        public T As<T>() where T : unmanaged, IExpressionNode {
            return *(T*)Unsafe.AsPointer(ref this);
        }

    }

    internal interface IVepProvider { }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BlockExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isUnsafe;

        public ExpressionRange statementList;
        public ExpressionNodeType NodeType => ExpressionNodeType.BlockExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.ReturnStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UsingStatement : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;

        public ExpressionIndex acquisition;
        public ExpressionIndex body;

        public ExpressionNodeType NodeType => ExpressionNodeType.UsingStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IfStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex condition;
        public ExpressionIndex body;
        public ExpressionIndex elseBody;

        public ExpressionNodeType NodeType => ExpressionNodeType.IfStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SwitchExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex lhs;
        public ExpressionRange<SwitchArm> switchArms;

        public ExpressionNodeType NodeType => ExpressionNodeType.SwitchExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SwitchStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex condition;
        public ExpressionRange<SwitchSection> sections;
        public bool hasCaseGuards;
        public ExpressionRange defaultBody;

        public bool hasDefault => defaultBody.length != 0;

        public ExpressionNodeType NodeType => ExpressionNodeType.SwitchStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SwitchArm : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex pattern;
        public ExpressionIndex guard;
        public ExpressionIndex body;

        public ExpressionNodeType NodeType => ExpressionNodeType.SwitchArm;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SwitchLabel : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex caseExpression;
        public ExpressionIndex guardExpression;

        public ExpressionNodeType NodeType => ExpressionNodeType.SwitchLabel;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SwitchSection : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<SwitchLabel> labels;
        public ExpressionRange bodyStatements;

        public ExpressionNodeType NodeType => ExpressionNodeType.SwitchSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WhileLoop : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex condition;
        public ExpressionIndex body;
        public bool isDoWhile;

        public ExpressionNodeType NodeType => ExpressionNodeType.WhileLoop;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ForLoop : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;

        public ExpressionIndex initializer; // not required, either a local_variable_definition or statement_expression_list
        public ExpressionIndex condition;
        public ExpressionRange iterator;
        public ExpressionIndex body; // embedded_statement

        public ExpressionNodeType NodeType => ExpressionNodeType.ForLoop;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ForeachLoop : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<VariableDeclaration> variableDeclaration;
        public ExpressionIndex enumerableExpression;
        public ExpressionIndex body;

        public ExpressionNodeType NodeType => ExpressionNodeType.ForeachLoop;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BreakStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionNodeType NodeType => ExpressionNodeType.BreakStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ContinueStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionNodeType NodeType => ExpressionNodeType.ContinueStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GoToStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex caseJumpTarget;
        public ExpressionIndex<Identifier> labelTarget;

        public bool jumpToDefault => !caseJumpTarget.IsValid && !labelTarget.IsValid;

        public ExpressionNodeType NodeType => ExpressionNodeType.GoToStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct YieldStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isReturn => expression.IsValid;
        public bool isBreak => !expression.IsValid;
        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.YieldStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TernaryExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex condition;
        public ExpressionIndex trueExpression;
        public ExpressionIndex falseExpression;

        public ExpressionNodeType NodeType => ExpressionNodeType.TernaryExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Argument : IExpressionNode, IVepProvider /* not sure if this is really a vep provider */ {

        public enum ArgumentModifier {

            None,
            Ref,
            In,
            Out

        }

        public ExpressionNodeHeader meta;

        public ArgumentModifier modifier;
        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.Argument;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ThrowStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.ThrowStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RangeExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        // according the grammar both sides can be optional.. weird
        public ExpressionIndex lhs;
        public ExpressionIndex rhs;

        public ExpressionNodeType NodeType => ExpressionNodeType.RangeExpression;

    }

    // in the specific case it can expose veps
    [StructLayout(LayoutKind.Sequential)]
    internal struct Catch : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;

        public ExpressionIndex exceptionFilter;
        public ExpressionIndex<BlockExpression> body;
        public NonTrivialTokenLocation identifier;
        public ExpressionIndex<TypePath> typePath;

        public bool isGeneral => !typePath.IsValid;

        public ExpressionNodeType NodeType => ExpressionNodeType.Catch;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TryCatchFinally : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<BlockExpression> tryBody;
        public ExpressionRange<Catch> catchClauses;
        public ExpressionIndex<BlockExpression> finallyClause;

        public ExpressionNodeType NodeType => ExpressionNodeType.TryCatchFinally;

    }

    internal enum BinaryPatternOp {

        Invalid,
        And,
        Or

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BinaryPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public BinaryPatternOp op;
        public ExpressionIndex lhs;
        public ExpressionIndex rhs;

        public ExpressionNodeType NodeType => ExpressionNodeType.BinaryPattern;

    }

    internal enum RelationalPatternOp {

        Invalid,
        NotEqual,
        Equal,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RelationalPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public RelationalPatternOp op;
        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.RelationalPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DiscardPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public NonTrivialTokenLocation location;
        public ExpressionNodeType NodeType => ExpressionNodeType.DiscardPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ConstantPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex expression;
        public ExpressionNodeType NodeType => ExpressionNodeType.ConstantPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VariableDesignation : IExpressionNode {

        public ExpressionNodeHeader meta;
        public bool isDiscard;
        // public ExpressionRange<VariableDesignation> designationList;  
        public ExpressionRange _designationList; // clr has a bug w/recursive type loading, mono does not  
        public NonTrivialTokenLocation singleDesignationLocation;

        public ExpressionRange<VariableDesignation> designationList {
            get => new ExpressionRange<VariableDesignation>(_designationList.start, _designationList.length);
            set => _designationList = new ExpressionRange(value.start, value.length);
        }

        public ExpressionNodeType NodeType => ExpressionNodeType.VariableDesignation;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DeclarationPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex<VariableDesignation> designation;

        public ExpressionNodeType NodeType => ExpressionNodeType.DeclarationPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VarPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex<VariableDesignation> designation;

        public ExpressionNodeType NodeType => ExpressionNodeType.VarPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypePattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex<TypePath> typePath;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypePattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnaryNotPattern : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex pattern;

        public ExpressionNodeType NodeType => ExpressionNodeType.UnaryNotPattern;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LockStatement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex lockExpression;
        public ExpressionIndex bodyExpression;

        public ExpressionNodeType NodeType => ExpressionNodeType.LockStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BracketExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isNullable;
        public ExpressionRange arguments;

        public ExpressionNodeType NodeType => ExpressionNodeType.BracketExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DefaultExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<TypePath> typePath;

        public ExpressionNodeType NodeType => ExpressionNodeType.DefaultExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SizeOfExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<TypePath> typePath;

        public ExpressionNodeType NodeType => ExpressionNodeType.SizeOfExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NameOfExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<Identifier> identifier;
        // todo -- need a chain here, not just 1 identifier, also probably needs to support type paths & unbound paths 

        public ExpressionNodeType NodeType => ExpressionNodeType.NameOfExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AnonymousMethodExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionNodeType NodeType => ExpressionNodeType.AnonymousMethodExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CheckedExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isChecked;

        public ExpressionNodeType NodeType => ExpressionNodeType.CheckedExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BaseAccessExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<Identifier> identifier;
        public ExpressionRange indexExpressions;

        public bool isIndexAccess => indexExpressions.length != 0;

        public ExpressionNodeType NodeType => ExpressionNodeType.BaseAccessExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TupleExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<Argument> arguments;

        public ExpressionNodeType NodeType => ExpressionNodeType.TupleExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Identifier : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;

        public IdentifierType identifierType;

        public ExpressionRange<TypePath> typeArgumentList;

        // this is to make it easier to print the right thing when there is a type argument list
        public NonTrivialTokenLocation identifierTokenIndex;

        public ExpressionNodeType NodeType => ExpressionNodeType.Identifier;

        public unsafe void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            builder.AppendInline("    " + tree->GetTokenSourceString(new NonTrivialTokenRange(identifierTokenIndex.index, identifierTokenIndex.index + 1)));
            if (typeArgumentList.length > 0) {
                NonTrivialTokenRange tokenRange = tree->GetTokenRange(typeArgumentList);
                builder.AppendInline("<");
                builder.AppendInline(tree->GetTokenSourceString(tokenRange));
                builder.AppendInline(">");
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeModifier : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isNullable;
        public bool isPointer;
        public int arrayRank;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypeModifier;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypePath : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<TypeNamePart> baseTypePath;
        public ExpressionRange<TypeModifier> modifiers;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypePath;

        public unsafe void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            builder.AppendInline("    ");
            builder.AppendInline(tree->GetTokenSourceString(nodeIndex));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeNamePart : IExpressionNode {

        public ExpressionNodeHeader meta;

        public SimpleTypeName simpleTypeName;
        public ExpressionRange _argumentList; // these are ExpressionRange<TypePath>, CLR but won't let us load them though 
        public ExpressionRange  _partList; // these are ExpressionRange<TypeNamePart>, CLR but won't let us load them though
        // public ExpressionRange<TypePath> argumentList;
        // public ExpressionRange<TypeNamePart> partList;
        public NonTrivialTokenLocation identifierLocation;

        public ExpressionRange<TypeNamePart> partList {
            get => new ExpressionRange<TypeNamePart>(_partList.start, _partList.length);
            set => _partList = new ExpressionRange(value.start, value.length);
        }
        
        public ExpressionRange<TypePath> argumentList {
            get => new ExpressionRange<TypePath>(_argumentList.start, _argumentList.length);
            set => _argumentList = new ExpressionRange(value.start, value.length);
        }
        
        public ExpressionNodeType NodeType => ExpressionNodeType.TypeNamePart;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DirectCast : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.DirectCast;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AssignmentExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex lhs;
        public ExpressionIndex rhs;
        public AssignmentOperatorType assignmentOperatorType;

        public bool isCoalesce => assignmentOperatorType == AssignmentOperatorType.CoalesceAssignment;

        public ExpressionNodeType NodeType => ExpressionNodeType.AssignmentExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ParenExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.ParenExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MethodInvocation : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<Argument> argumentList;

        public ExpressionNodeType NodeType => ExpressionNodeType.MethodInvocation;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IncrementDecrement : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isIncrement;
        public ExpressionNodeType NodeType => ExpressionNodeType.IncrementDecrement;

    }

    internal enum PrimaryExpressionPartType {

        MemberAccess,
        MethodInvocation,
        IncrementDecrement

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PrimaryExpressionPart : IExpressionNode {

        public ExpressionNodeHeader meta;
        public PrimaryExpressionPartType partType;
        public ExpressionIndex expression;
        public ExpressionRange<BracketExpression> bracketExpressions;

        public ExpressionNodeType NodeType => ExpressionNodeType.PrimaryExpressionPart;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NewExpression : IExpressionNode {

        public ExpressionNodeHeader meta;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionRange<Argument> argList;
        public ExpressionRange<ArrayCreationRank> arraySpecs;
        public ExpressionIndex initializer;

        public ExpressionNodeType NodeType => ExpressionNodeType.NewExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArrayCreationRank : IExpressionNode {

        public ExpressionNodeHeader meta;

        public int rank;
        public ExpressionRange expressionList;

        public ExpressionNodeType NodeType => ExpressionNodeType.ArrayCreationRank;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PrimaryExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex start;
        public ExpressionRange<PrimaryExpressionPart> parts;
        public ExpressionRange<BracketExpression> bracketExpressions;

        public ExpressionNodeType NodeType => ExpressionNodeType.PrimaryExpression;

    }

    // these are the identifiers that start expression chains, ie the ones we need to do identifier resolution on
    [StructLayout(LayoutKind.Sequential)]
    internal struct PrimaryIdentifier : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<TypePath> typeArgumentList;

        // this is to make it easier to print the right thing when there is a type argument list
        public NonTrivialTokenLocation identifierLocation;
        public SimpleTypeName simpleTypeName;

        public unsafe void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            builder.AppendInline("    " + tree->GetTokenSourceString(identifierLocation));
        }

        public ExpressionNodeType NodeType => ExpressionNodeType.PrimaryIdentifier;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnboundTypeNameExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public int genericDegree;
        public NonTrivialTokenLocation nameLocation;
        public ExpressionIndex _next; // clr has a bug with recursive type definition here
        // public ExpressionIndex<UnboundTypeNameExpression> next;

        public ExpressionIndex<UnboundTypeNameExpression> next {
            get => new ExpressionIndex<UnboundTypeNameExpression>(_next.id);
            set => _next = new ExpressionIndex(value.id);
        }
        
        public ExpressionNodeType NodeType => ExpressionNodeType.UnboundTypeNameExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeOfExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isVoidType;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionRange<UnboundTypeNameExpression> unboundTypeName;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypeOfExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PrefixUnaryExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public PrefixOperator prefixOperator;
        public ExpressionIndex expression;

        public ExpressionNodeType NodeType => ExpressionNodeType.PrefixUnaryExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LambdaExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isAsync;
        public ExpressionRange<Parameter> parameters;
        public ExpressionIndex body;
        public bool hasFormalParameters;

        public ExpressionNodeType NodeType => ExpressionNodeType.LambdaExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BinaryExpression : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;
        public BinaryOperatorType operatorType;

        public ExpressionIndex lhs;
        public ExpressionIndex rhs;

        public ExpressionNodeType NodeType => ExpressionNodeType.BinaryExpression;

        public void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            builder.AppendInline(" " + operatorType.ToString());
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IsTypeExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isNegated;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionRange<TypePatternArm> typePatternArms;
        public NonTrivialTokenLocation identifier;

        public ExpressionNodeType NodeType => ExpressionNodeType.IsTypeExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IsNullExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public bool isNegated;
        public ExpressionNodeType NodeType => ExpressionNodeType.IsNullExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypePatternArm : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;
        public ExpressionIndex expression;
        public ExpressionIndex<Identifier> identifier;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypePatternArm;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypeArgumentList : IExpressionNode {

        public ExpressionNodeHeader meta;

        // todo -- probably remove this and just use ExpressionRange<TypePath> 

        public ExpressionRange<TypePath> typePaths;

        public ExpressionNodeType NodeType => ExpressionNodeType.TypeArgumentList;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemberAccess : IExpressionNode {

        public ExpressionNodeHeader meta;

        // maybe remove the identifier from this and just take a token index?

        public bool isConditionalAccess;
        public ExpressionIndex<Identifier> identifier;
        public ExpressionIndex<TypeArgumentList> argumentList;

        public ExpressionNodeType NodeType => ExpressionNodeType.MemberAccess;

    }

    // maybe just merge w/ VariableInitializer & add a type field if needed
    [StructLayout(LayoutKind.Sequential)]
    internal struct ArrayInitializer : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange initializers;

        public ExpressionNodeType NodeType => ExpressionNodeType.ArrayInitializer;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LocalFunctionDefinition : IExpressionNode {

        public ExpressionNodeHeader meta;

        public FunctionTypeModifiers modifiers;
        public ExpressionRange<Identifier> typeParameters;
        public ExpressionIndex<BlockExpression> body;
        public ExpressionIndex<TypePath> returnType;
        public ExpressionRange<Parameter> parameters;
        public NonTrivialTokenLocation nameLocation;

        public ExpressionNodeType NodeType => ExpressionNodeType.LocalFunctionDefinition;

    }

    [Flags]
    internal enum FunctionTypeModifiers {

        None,
        Async = 1 << 0,
        Unsafe = 1 << 1,
        Static = 1 << 2

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VariableDeclaration : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;
        public VariableModifiers modifiers;
        public VariableDeclarationType declarationType;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public NonTrivialTokenLocation identifierLocation;

        public ExpressionNodeType NodeType => ExpressionNodeType.VariableDeclaration;

    }

    internal enum VariableModifiers {

        None,
        Ref,

    }

    internal enum VariableDeclarationType {

        Invalid,
        Const,
        ImplicitVar,
        TypedVar

    }

    internal enum ArgumentModifier {

        None,
        Ref,
        Out,
        In

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LiteralAccess : IExpressionNode {

        public ExpressionNodeHeader meta;

        public LiteralType literalType;

        // "string".Length or 10.ToString() 
        // this only handles the head of the expression, PrimaryExpression will handle the rest as needed 
        public ExpressionIndex<Identifier> access;
        public ExpressionNodeType NodeType => ExpressionNodeType.LiteralAccess;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Literal : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;
        public LiteralType literalType;

        public ExpressionNodeType NodeType => ExpressionNodeType.Literal;

        public unsafe void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            builder.AppendInline(" " + tree->GetTokenSourceString(nodeIndex));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StringInterpolation : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange parts;
        public ExpressionNodeType NodeType => ExpressionNodeType.StringInterpolation;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StringInterpolationPart : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionIndex expression;
        public ExpressionIndex alignmentExpression;
        public NonTrivialTokenLocation formatDirective;

        public ExpressionNodeType NodeType => ExpressionNodeType.StringInterpolationPart;

    }

    internal enum ElementInitializerType {

        PropertyMember,
        FieldMember,
        SingleElement,
        CurlyBraceList,
        Indexer

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ElementInitializer : IExpressionNode {

        public ExpressionNodeHeader meta;

        // one or the other will be used 
        public ExpressionIndex expression;
        public ExpressionRange expressionList;
        public ElementInitializerType initializerType;

        public ExpressionNodeType NodeType => ExpressionNodeType.ElementInitializer;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MemberInitializer : IExpressionNode, IPrintableExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange lhsExpressionList;
        public ExpressionIndex rhs;
        public NonTrivialTokenLocation lhsIdentifier;
        public ElementInitializerType initializerType;

        public ExpressionNodeType NodeType => ExpressionNodeType.MemberInitializer;

        public unsafe void Print(IndentedStringBuilder builder, ExpressionTree* tree, ExpressionIndex nodeIndex) {
            if (lhsIdentifier.IsValid) {
                builder.AppendInline("    " + tree->GetTokenSourceString(lhsIdentifier));
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CollectionInitializer : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<ElementInitializer> initializers;

        public ExpressionNodeType NodeType => ExpressionNodeType.CollectionInitializer;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectInitializer : IExpressionNode {

        public ExpressionNodeHeader meta;

        public ExpressionRange<MemberInitializer> memberInit;

        public ExpressionNodeType NodeType => ExpressionNodeType.ObjectInitializer;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ResolveIdExpression : IExpressionNode {

        public ExpressionNodeHeader meta;

        public NonTrivialTokenRange moduleName;
        public NonTrivialTokenRange tagName;
        
        public ExpressionNodeType NodeType => ExpressionNodeType.ResolveIdExpression;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Parameter : IExpressionNode, IVepProvider {

        public ExpressionNodeHeader meta;

        public bool isExplicit;
        public ArgumentModifier modifier;
        public ExpressionIndex defaultExpression;
        public ExpressionIndex<TypePath> typePath;
        public NonTrivialTokenLocation nameLocation;
        public ExpressionNodeType NodeType => ExpressionNodeType.Parameter;

    }

}