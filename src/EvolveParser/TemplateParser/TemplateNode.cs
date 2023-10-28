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
    public struct UntypedTemplateNode {

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

    public unsafe struct UntypedTemplateNodeData {

        public fixed byte bytes[32]; // largest size right now 

    }

    public interface ITemplateNode {

        TemplateNodeType NodeType { get; }

    }

    public struct NodeIndex<T> where T : unmanaged, ITemplateNode {

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

    public struct NodeIndex {

        public int id;

        [DebuggerStepThrough]
        public NodeIndex(int id) {
            this.id = id;
        }

        public bool IsValid => id > 0;

    }

    public struct NodeRange {

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

    public struct NodeRange<T> where T : unmanaged, ITemplateNode {

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

    public enum ScopeModifier {

        None,
        Destructive,
        NonDestructive

    }

    public struct NodeRangeIndex {

        public int index;

        public NodeRangeIndex(int index) {
            this.index = index;
        }

    }

    public struct NodeRangeIndex<T> where T : unmanaged, ITemplateNode {

        public int index;

        [DebuggerStepThrough]
        public NodeRangeIndex(int index) {
            this.index = index;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TemplateNodeMeta {

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
    public struct TemplateBlockNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateBlockNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateSwitchStatement : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex condition;
        public NodeRange<TemplateSwitchSection> sections;
        public ScopeModifier modifier;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchStatement;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateIfStatement : ITemplateNode {

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
    public struct StyleListNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange<ResolveIdExpression> styleIds;

        public TemplateNodeType NodeType => TemplateNodeType.StyleListNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ElementNode : ITemplateNode, IPrintableTemplateNode {

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
    public struct TemplateElementAssignment : ITemplateNode {

        public TemplateNodeMeta meta;
        public int identifierLocation;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateElementAssignment;

    }

    public enum SlotType {

        Invalid,
        Named,
        Dynamic,
        Implicit,

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateSlotSignature : ITemplateNode {

        public TemplateNodeMeta meta;
        public SlotType slotType;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionRange<Parameter> parameters;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSlotSignature;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RepeatNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex<Identifier> itemIdentifier;
        public ExpressionIndex repeatExpression;
        public NodeRange<RepeatParameter> parameters;
        public NodeRange<Extrusion> extrusions;
        public NodeIndex body;

        public TemplateNodeType NodeType => TemplateNodeType.RepeatNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateFunctionSignature : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateFunctionSignature;

    }

    public enum TemplateFnParameterModifier {

        None,
        Ref,
        Out // not sure if I can actually support this 

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateFunctionParameter : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateFnParameterModifier modifier;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex defaultValue;
        public ExpressionIndex<TypePath> typePath;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateFunctionParameter;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LocalTemplateFn : ITemplateNode, IPrintableTemplateNode {

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
    public struct CreateSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.CreateSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EnableSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.EnableSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RunSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionRange statements;

        public TemplateNodeType NodeType => TemplateNodeType.RunSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MarkerNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeIndex<TemplateBlockNode> block;
        public bool isDeferred;
        public NonTrivialTokenLocation identifer;

        public TemplateNodeType NodeType => TemplateNodeType.MarkerNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TeleportNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeIndex<TemplateBlockNode> block;
        public ExpressionIndex portalExpression;
        public ExpressionIndex searchExpression;

        public TemplateNodeType NodeType => TemplateNodeType.TeleportNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyBindingNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public TemplateArgumentModifier modifier;
        public NonTrivialTokenLocation parameterName;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.PropertyBindingNode;

    }

    public enum TemplateArgumentModifier {

        None = 0,
        Sync,
        Const

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DecoratorArgumentNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation name;
        public ExpressionIndex expression;
        public TemplateArgumentModifier modifier;

        public TemplateNodeType NodeType => TemplateNodeType.DecoratorArgumentNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ComputedProperty : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public NonTrivialTokenLocation alias;
        public ExpressionIndex<TypePath> typePath;
        public bool isPublic;

        public TemplateNodeType NodeType => TemplateNodeType.ComputedProperty;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateParameter : ITemplateNode {

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
    public struct FromMapping : ITemplateNode {

        public TemplateNodeMeta meta;

        public NonTrivialTokenLocation start;
        public ExpressionRange<Identifier> chain;

        public TemplateNodeType NodeType => TemplateNodeType.FromMapping;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OnChangeBindingNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifier;
        public ExpressionIndex expression;
        public NodeIndex<FromMapping> fromMapping;

        public TemplateNodeType NodeType => TemplateNodeType.OnChangeBindingNode;

    }

    public struct InputModifiers {

        public bool requireFocus;
        public InputPhase eventPhase;
        public KeyboardModifiers keyboardModifiers;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LifeCycleEventNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public LifeCycleEventType eventType;
        public ExpressionIndex expression;
        public NodeIndex<FromMapping> fromMapping;
        public TemplateNodeType NodeType => TemplateNodeType.LifeCycleEventNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InputHandlerNode : ITemplateNode {

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
    public struct InstanceStyleNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation stylePropertyName;
        public ExpressionIndex valueExpression;
        public bool isConst;

        public TemplateNodeType NodeType => TemplateNodeType.InstanceStyleNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AttributeAssignment : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenRange key;
        public ExpressionIndex value;
        public bool isConst;

        public TemplateNodeType NodeType => TemplateNodeType.AttributeAssignment;

    }

    public struct QualifiedIdentifier {

        public NonTrivialTokenLocation moduleName;
        public NonTrivialTokenLocation tagName;

        public unsafe string Print(TemplateTree* tree) {
            return moduleName.index < 0
                ? tree->GetTokenSourceString(tagName)
                : tree->GetTokenSourceString(new NonTrivialTokenRange(moduleName.index, tagName.index + 1));
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TypedQualifiedIdentifierNode : ITemplateNode {

        public TemplateNodeMeta meta;

        public NonTrivialTokenLocation moduleName;
        public NonTrivialTokenLocation tagName;
        public ExpressionRange<TypePath> typeArguments;

        public TemplateNodeType NodeType => TemplateNodeType.TypedQualifiedIdentifierNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Extrusion : ITemplateNode, IPrintableTemplateNode {

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
    public struct KeyedArgument : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public TemplateNodeType NodeType => TemplateNodeType.KeyedArgument;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DecoratorNode : ITemplateNode, IPrintableTemplateNode {

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
    public struct TemplateSignatureNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange arguments;
        public ExpressionIndex<TypePath> companionTypePath;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSignatureNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateConstDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateConstDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateVariableDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateVariableDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateSpawnList : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<TypedQualifiedIdentifierNode> spawnList;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateSpawnList;

    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateStateDeclaration : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation identifierLocation;
        public ExpressionIndex<TypePath> typePath;
        public ExpressionIndex initializer;
        public bool isPublic;
        public NodeIndex<FromMapping> fromMapping;
        public TemplateNodeType NodeType => TemplateNodeType.TemplateStateDeclaration;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MethodDeclaration : ITemplateNode {

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
    public struct RepeatParameter : ITemplateNode {

        public TemplateNodeMeta meta;
        public RepeatParameterName key;
        public ExpressionIndex value;

        public TemplateNodeType NodeType => TemplateNodeType.RepeatParameter;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderSlotNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NonTrivialTokenLocation nameLocation;
        public ExpressionRange parameters;
        public NodeIndex<TemplateBlockNode> defaultBlock;
        public ExpressionIndex dynamicExpression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderSlotNode;

        public bool IsDynamicSlot => dynamicExpression.IsValid;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderPortalNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderPortalNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderMarkerNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;

        public TemplateNodeType NodeType => TemplateNodeType.RenderMarkerNode;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateSwitchSection : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<TemplateSwitchLabel> labels;
        public NodeIndex body;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchSection;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TemplateSwitchLabel : ITemplateNode {

        public TemplateNodeMeta meta;
        public ExpressionIndex expression;
        public bool isDefault;

        public TemplateNodeType NodeType => TemplateNodeType.TemplateSwitchLabel;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SlotOverrideNode : ITemplateNode {

        public TemplateNodeMeta meta;
        public NodeRange<Extrusion> extrusions;
        public NonTrivialTokenLocation slotName;
        public NodeIndex contents;
        public ExpressionIndex slotExpression;

        public SlotType slotType;

        public TemplateNodeType NodeType => TemplateNodeType.SlotOverrideNode;

    }

}
