using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EvolveUI.Util;

namespace EvolveUI.Parsing {

    internal unsafe interface IPrintableTemplateNode {

        void Print(IndentedStringBuilder builder, TemplateTree* tree, NodeIndex nodeIndex);

    }

    [DebuggerDisplay("{GetDebugString()}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct UntypedTemplateNode {

        public TemplateNodeMeta meta;
        private UntypedTemplateNodeData data;

        public string GetDebugString() {
            return $"{meta.nodeType}";
        }

        [DebuggerStepThrough]
        public unsafe T As<T>() where T : unmanaged, ITemplateNode {
            return *(T*)Unsafe.AsPointer(ref this);
        }

        public unsafe bool TryConvert<T>(out T result) where T : unmanaged, ITemplateNode {
            result = *(T*)Unsafe.AsPointer(ref this);
            return meta.nodeType == result.NodeType;
        }

    }

    internal unsafe struct UntypedTemplateNodeData {

        public fixed byte bytes[32]; // largest size right now 

    }

    internal interface ITemplateNode {

        TemplateNodeType NodeType { get; }

    }

    internal struct NodeIndex<T> where T : unmanaged, ITemplateNode {

        public int id;

        public bool IsValid => id > 0;

        [DebuggerStepThrough]
        public NodeIndex(int id) {
            this.id = id;
        }

        [DebuggerStepThrough]
        public NodeIndex(NodeIndex id) {
            this.id = id.id;
        }

        [DebuggerStepThrough]
        public static implicit operator NodeIndex(NodeIndex<T> self) {
            return new NodeIndex(self.id);
        }

    }

    internal struct NodeIndex {

        public int id;

        [DebuggerStepThrough]
        public NodeIndex(int id) {
            this.id = id;
        }

        public bool IsValid => id > 0;

    }

    internal struct NodeRange {

        public ushort start;
        public ushort length;

        public bool IsValid => start > 0;

        public NodeRangeIndex this[int offset] {
            [DebuggerStepThrough] get => new NodeRangeIndex(start + offset);
        }

        public NodeRange Slice(int newStart) {
            return new NodeRange() {
                start = (ushort)(start + newStart),
                length = (ushort)(length - newStart)
            };
        }

    }

    internal struct NodeRange<T> where T : unmanaged, ITemplateNode {

        public ushort start;
        public ushort length;

        public bool IsValid => start > 0;

        public NodeRangeIndex<T> this[int offset] {
            [DebuggerStepThrough] get => new NodeRangeIndex<T>(start + offset);
        }

        [DebuggerStepThrough]
        public static implicit operator NodeRange(NodeRange<T> self) {
            return new NodeRange() { start = self.start, length = self.length };
        }

    }

    internal enum ScopeModifier {

        None,
        Destructive,
        NonDestructive

    }

    internal struct NodeRangeIndex {

        public int index;

        public NodeRangeIndex(int index) {
            this.index = index;
        }

    }

    internal struct NodeRangeIndex<T> where T : unmanaged, ITemplateNode {

        public int index;

        [DebuggerStepThrough]
        public NodeRangeIndex(int index) {
            this.index = index;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TemplateNodeMeta {

        public TemplateNodeType nodeType;
        public ushort nodeIndexShort;

        public NonTrivialTokenRange tokenRange;

        public NodeIndex nodeIndex => new NodeIndex(nodeIndexShort);

        [DebuggerStepThrough]
        public TemplateStructureType GetStructureType() {
            return (int)nodeType >= 100
                ? TemplateStructureType.None
                : (TemplateStructureType)nodeType;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateBlockNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateBlockNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSwitchStatement : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex condition;
        public NodeRange<TemplateSwitchSection> sections;
        public ScopeModifier modifier;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateIfStatement : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex condition;
        public NodeIndex trueBody;
        public NodeIndex falseBody;
        public ScopeModifier scopeModifier;
        public int bodyStartIndex;
        public int bodyEndIndex;
        public int elseEndIndex;
        public bool isElseIf;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateIfStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StyleListNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange<ResolveIdExpression> styleIds;

        public TemplateNodeType NodeType => TemplateNodeType.StyleListNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ElementNode : ITemplateNode, IPrintableTemplateNode {

        public TemplateNodeMeta meta;
        public QualifiedIdentifier qualifiedName;
        public NodeRange<DecoratorNode> decorators;
        public NodeRange bindings;
        public NodeRange<Extrusion> extrusions;
        public NodeIndex<TemplateBlockNode> childBlock;
        public ExpressionRange<TypePath> typeArguments;

        public TemplateNodeType NodeType => TemplateNodeType.ElementNode;

        public unsafe void Print(IndentedStringBuilder builder, TemplateTree* tree, NodeIndex nodeIndex) {
            builder.AppendInline("    ");
            builder.AppendInline(qualifiedName.Print(tree));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateElementAssignment : ITemplateNode {

        public TemplateNodeMeta meta;
        public int identifierLocation;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateElementAssignment;

    }

    internal enum SlotType {

        Invalid,
        Named,
        Dynamic,
        Implicit,

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSlotSignature : ITemplateNode {

        public TemplateNodeMeta meta;
        public SlotType slotType;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionRange<Parameter> parameters;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSlotSignature;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RepeatNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex<Identifier> itemIdentifier;
        public ExpressionIndex repeatExpression;
        public NodeRange<RepeatParameter> parameters;
        public NodeRange<Extrusion> extrusions;
        public NodeIndex body;

        public TemplateNodeType NodeType => TemplateNodeType.RepeatNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateFunctionSignature : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateFunctionSignature;

    }

    internal enum TemplateFnParameterModifier {

        None,
        Ref,
        Out // not sure if I can actually support this 

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateFunctionParameter : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateFnParameterModifier modifier;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex defaultValue;
        public ExpressionIndex<TypePath> typePath;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateFunctionParameter;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LocalTemplateFn : ITemplateNode, IPrintableTemplateNode {

        public TemplateNodeMeta meta;
        public bool isStatic;
        public NonTrivialTokenLocation identifier;
        public ExpressionRange<Identifier> typeParameters;
        public NodeRange<TemplateFunctionParameter> parameters;
        public NodeIndex<TemplateFunctionSignature> signature;
        public NodeIndex<TemplateBlockNode> body;

        public TemplateNodeType NodeType => TemplateNodeType.LocalTemplateFn;

        public unsafe void Print(IndentedStringBuilder builder, TemplateTree* tree, NodeIndex nodeIndex) {
            builder.AppendInline("    " + tree->GetTokenSourceString(identifier));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.CreateSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EnableSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.EnableSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RunSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.RunSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MarkerNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeIndex<TemplateBlockNode> block;
        public bool isDeferred;
        public NonTrivialTokenLocation identifer;

        public TemplateNodeType NodeType => TemplateNodeType.MarkerNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TeleportNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeIndex<TemplateBlockNode> block;
        public ExpressionIndex portalExpression;
        public ExpressionIndex searchExpression;

        public TemplateNodeType NodeType => TemplateNodeType.TeleportNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyBindingNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateArgumentModifier modifier;
        public NonTrivialTokenLocation parameterName;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.PropertyBindingNode;

    }

    internal enum TemplateArgumentModifier {

        None = 0,
        Sync,
        Const

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DecoratorArgumentNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation name;
        public ExpressionIndex expression;
        public TemplateArgumentModifier modifier;

        public TemplateNodeType NodeType => TemplateNodeType.DecoratorArgumentNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComputedProperty : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public NonTrivialTokenLocation alias;
        public ExpressionIndex<TypePath> typePath;
        public bool isPublic;

        public TemplateNodeType NodeType => TemplateNodeType.ComputedProperty;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateParameter : ITemplateNode {

        public TemplateNodeMeta meta;
        public ArgumentModifier modifier;
        public ExpressionIndex defaultValue;
        public NonTrivialTokenLocation identifier;
        public ExpressionIndex<TypePath> typePath;
        public NodeIndex<FromMapping> fromMapping;
        public bool isPublic;
        public bool isRequired;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateParameter;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FromMapping : ITemplateNode {

        public TemplateNodeMeta meta;

        public NonTrivialTokenLocation start;
        public ExpressionRange<Identifier> chain;

        public TemplateNodeType NodeType => TemplateNodeType.FromMapping;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OnChangeBindingNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifier;
        public ExpressionIndex expression;
        public NodeIndex<FromMapping> fromMapping;

        public TemplateNodeType NodeType => TemplateNodeType.OnChangeBindingNode;

    }

    internal struct InputModifiers {

        public bool requireFocus;
        public InputPhase eventPhase;
        public KeyboardModifiers keyboardModifiers;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LifeCycleEventNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public LifeCycleEventType eventType;
        public ExpressionIndex expression;
        public NodeIndex<FromMapping> fromMapping;
        public TemplateNodeType NodeType => TemplateNodeType.LifeCycleEventNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InputHandlerNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public InputModifiers inputModifiers;
        public InputEventType inputEventType;
        public NodeIndex<FromMapping> fromMapping;
        public ExpressionIndex<TypePath> dragEventType;

        public bool isMouseEvent => (inputEventType & InputEventType.MOUSE_EVENT) != 0;
        public bool isKeyEvent => (inputEventType & InputEventType.KEY_EVENT) != 0;
        public bool isDragEvent => (inputEventType & InputEventType.DRAG_EVENT) != 0;
        public bool isFocusEvent => (inputEventType & InputEventType.FOCUS_EVENT) != 0;

        public TemplateNodeType NodeType => TemplateNodeType.InputHandlerNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceStyleNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation stylePropertyName;
        public ExpressionIndex valueExpression;
        public bool isConst;

        public TemplateNodeType NodeType => TemplateNodeType.InstanceStyleNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AttributeAssignment : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenRange key;
        public ExpressionIndex value;
        public bool isConst;

        public TemplateNodeType NodeType => TemplateNodeType.AttributeAssignment;

    }

    internal struct QualifiedIdentifier {

        public NonTrivialTokenLocation moduleName;
        public NonTrivialTokenLocation tagName;

        public unsafe string Print(TemplateTree* tree) {
            return moduleName.index < 0
                ? tree->GetTokenSourceString(tagName)
                : tree->GetTokenSourceString(new NonTrivialTokenRange(moduleName.index, tagName.index + 1));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TypedQualifiedIdentifierNode : ITemplateNode {

        public TemplateNodeMeta meta;

        public NonTrivialTokenLocation moduleName;
        public NonTrivialTokenLocation tagName;
        public ExpressionRange<TypePath> typeArguments;

        public TemplateNodeType NodeType => TemplateNodeType.TypedQualifiedIdentifierNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Extrusion : ITemplateNode, IPrintableTemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public NonTrivialTokenLocation alias;
        public bool isDiscard;
        public bool isElementExtrusion;
        public bool isLifted;

        public unsafe void Print(IndentedStringBuilder builder, TemplateTree* tree, NodeIndex nodeIndex) {
            builder.AppendInline("    ");
            UntypedTemplateNode node = tree->Get(nodeIndex);
            builder.AppendInline(tree->GetTokenSourceString(node.meta.tokenRange));
        }

        public TemplateNodeType NodeType => TemplateNodeType.Extrusion;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyedArgument : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public TemplateNodeType NodeType => TemplateNodeType.KeyedArgument;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DecoratorNode : ITemplateNode, IPrintableTemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<DecoratorArgumentNode> arguments;
        public NodeRange<Extrusion> extrusions;
        public QualifiedIdentifier qualifiedName;
        public ExpressionRange<TypePath> typeArguments;
        public TemplateNodeType NodeType => TemplateNodeType.DecoratorNode;

        public unsafe void Print(IndentedStringBuilder builder, TemplateTree* tree, NodeIndex nodeIndex) {
            builder.AppendInline("    ");
            builder.AppendInline(tree->expressionTree->GetTokenSourceString(qualifiedName.tagName));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSignatureNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange arguments;
        public ExpressionIndex<TypePath> companionTypePath;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSignatureNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateConstDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateConstDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateVariableDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateVariableDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSpawnList : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<TypedQualifiedIdentifierNode> spawnList;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateSpawnList;

    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateStateDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public bool isPublic;
        public NodeIndex<FromMapping> fromMapping;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateStateDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MethodDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;

        // will be invalid if returning void
        public ExpressionIndex<TypePath> returnType;
        public NodeRange<TemplateFunctionParameter> parameters;
        public ExpressionIndex body;
        public NonTrivialTokenLocation identifierLocation;
        public bool isPublic;
        public NodeIndex<FromMapping> fromMapping;

        public TemplateNodeType NodeType => TemplateNodeType.MethodDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RepeatParameter : ITemplateNode {

        public TemplateNodeMeta meta;
        public RepeatParameterName key;
        public ExpressionIndex value;

        public TemplateNodeType NodeType => TemplateNodeType.RepeatParameter;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RenderSlotNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation nameLocation;
        public ExpressionRange parameters;
        public NodeIndex<TemplateBlockNode> defaultBlock;
        public ExpressionIndex dynamicExpression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderSlotNode;

        public bool IsDynamicSlot => dynamicExpression.IsValid;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RenderPortalNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderPortalNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RenderMarkerNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderMarkerNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSwitchSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<TemplateSwitchLabel> labels;
        public NodeIndex body;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TemplateSwitchLabel : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public bool isDefault;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchLabel;

    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SlotOverrideNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<Extrusion> extrusions;
        public NonTrivialTokenLocation slotName;
        public NodeIndex contents;
        public ExpressionIndex slotExpression;

        public SlotType slotType;

        public TemplateNodeType NodeType => TemplateNodeType.SlotOverrideNode;

    }

}