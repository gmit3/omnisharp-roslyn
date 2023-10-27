using System;
using System.Diagnostics;
using EvolveUI.Util.Unsafe;
using System.Text;
using EvolveUI.Util;

namespace EvolveUI.Parsing {

    internal unsafe partial struct TemplateFile : IDisposable {
        public const int Version = 44;    
    }

    internal unsafe partial struct TemplatePrintingVisitor {
        
        partial void VisitImpl(IndentedStringBuilder builder, NodeIndex index) {
            
             if(index.id <= 0) return;
             
             UntypedTemplateNode node = tree->Get(index);
             
             switch (node.meta.nodeType) {

                case TemplateNodeType.TemplateBlockNode: {
                    TemplateBlockNode data = node.As<TemplateBlockNode>();
                    builder.Append("TemplateBlockNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.statements.start, data.statements.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSwitchStatement: {
                    TemplateSwitchStatement data = node.As<TemplateSwitchStatement>();
                    builder.Append("TemplateSwitchStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.condition.id);
                    VisitRange(builder, data.sections.start, data.sections.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateIfStatement: {
                    TemplateIfStatement data = node.As<TemplateIfStatement>();
                    builder.Append("TemplateIfStatement");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.condition.id);
                    VisitImpl(builder, data.trueBody);
                    VisitImpl(builder, data.falseBody);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.StyleListNode: {
                    StyleListNode data = node.As<StyleListNode>();
                    builder.Append("StyleListNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.styleIds.start, data.styleIds.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.ElementNode: {
                    ElementNode data = node.As<ElementNode>();
                    builder.Append("ElementNode");
                    data.Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.decorators.start, data.decorators.length);
                    VisitRange(builder, data.bindings.start, data.bindings.length);
                    VisitRange(builder, data.extrusions.start, data.extrusions.length);
                    VisitImpl(builder, data.childBlock);
                    VisitExpressionRange(builder, data.typeArguments.start, data.typeArguments.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateElementAssignment: {
                    TemplateElementAssignment data = node.As<TemplateElementAssignment>();
                    builder.Append("TemplateElementAssignment");
                    builder.Indent();
                    builder.NewLine();
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSlotSignature: {
                    TemplateSlotSignature data = node.As<TemplateSlotSignature>();
                    builder.Append("TemplateSlotSignature");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.parameters.start, data.parameters.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RepeatNode: {
                    RepeatNode data = node.As<RepeatNode>();
                    builder.Append("RepeatNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.itemIdentifier.id);
                    VisitExpression(builder, data.repeatExpression.id);
                    VisitRange(builder, data.parameters.start, data.parameters.length);
                    VisitRange(builder, data.extrusions.start, data.extrusions.length);
                    VisitImpl(builder, data.body);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateFunctionSignature: {
                    TemplateFunctionSignature data = node.As<TemplateFunctionSignature>();
                    builder.Append("TemplateFunctionSignature");
                    builder.Indent();
                    builder.NewLine();
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateFunctionParameter: {
                    TemplateFunctionParameter data = node.As<TemplateFunctionParameter>();
                    builder.Append("TemplateFunctionParameter");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.defaultValue.id);
                    VisitExpression(builder, data.typePath.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.LocalTemplateFn: {
                    LocalTemplateFn data = node.As<LocalTemplateFn>();
                    builder.Append("LocalTemplateFn");
                    data.Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.typeParameters.start, data.typeParameters.length);
                    VisitRange(builder, data.parameters.start, data.parameters.length);
                    VisitImpl(builder, data.signature);
                    VisitImpl(builder, data.body);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.CreateSection: {
                    CreateSection data = node.As<CreateSection>();
                    builder.Append("CreateSection");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.statements.start, data.statements.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.EnableSection: {
                    EnableSection data = node.As<EnableSection>();
                    builder.Append("EnableSection");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.statements.start, data.statements.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RunSection: {
                    RunSection data = node.As<RunSection>();
                    builder.Append("RunSection");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.statements.start, data.statements.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.MarkerNode: {
                    MarkerNode data = node.As<MarkerNode>();
                    builder.Append("MarkerNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data.block);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TeleportNode: {
                    TeleportNode data = node.As<TeleportNode>();
                    builder.Append("TeleportNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitImpl(builder, data.block);
                    VisitExpression(builder, data.portalExpression.id);
                    VisitExpression(builder, data.searchExpression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.PropertyBindingNode: {
                    PropertyBindingNode data = node.As<PropertyBindingNode>();
                    builder.Append("PropertyBindingNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.DecoratorArgumentNode: {
                    DecoratorArgumentNode data = node.As<DecoratorArgumentNode>();
                    builder.Append("DecoratorArgumentNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.ComputedProperty: {
                    ComputedProperty data = node.As<ComputedProperty>();
                    builder.Append("ComputedProperty");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    VisitExpression(builder, data.typePath.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateParameter: {
                    TemplateParameter data = node.As<TemplateParameter>();
                    builder.Append("TemplateParameter");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.defaultValue.id);
                    VisitExpression(builder, data.typePath.id);
                    VisitImpl(builder, data.fromMapping);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.FromMapping: {
                    FromMapping data = node.As<FromMapping>();
                    builder.Append("FromMapping");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.chain.start, data.chain.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.OnChangeBindingNode: {
                    OnChangeBindingNode data = node.As<OnChangeBindingNode>();
                    builder.Append("OnChangeBindingNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    VisitImpl(builder, data.fromMapping);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.LifeCycleEventNode: {
                    LifeCycleEventNode data = node.As<LifeCycleEventNode>();
                    builder.Append("LifeCycleEventNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    VisitImpl(builder, data.fromMapping);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.InputHandlerNode: {
                    InputHandlerNode data = node.As<InputHandlerNode>();
                    builder.Append("InputHandlerNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    VisitImpl(builder, data.fromMapping);
                    VisitExpression(builder, data.dragEventType.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.InstanceStyleNode: {
                    InstanceStyleNode data = node.As<InstanceStyleNode>();
                    builder.Append("InstanceStyleNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.valueExpression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.AttributeAssignment: {
                    AttributeAssignment data = node.As<AttributeAssignment>();
                    builder.Append("AttributeAssignment");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.value.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TypedQualifiedIdentifierNode: {
                    TypedQualifiedIdentifierNode data = node.As<TypedQualifiedIdentifierNode>();
                    builder.Append("TypedQualifiedIdentifierNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.typeArguments.start, data.typeArguments.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.Extrusion: {
                    Extrusion data = node.As<Extrusion>();
                    builder.Append("Extrusion");
                    data.Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.KeyedArgument: {
                    KeyedArgument data = node.As<KeyedArgument>();
                    builder.Append("KeyedArgument");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.DecoratorNode: {
                    DecoratorNode data = node.As<DecoratorNode>();
                    builder.Append("DecoratorNode");
                    data.Print(builder, tree, index);
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.arguments.start, data.arguments.length);
                    VisitRange(builder, data.extrusions.start, data.extrusions.length);
                    VisitExpressionRange(builder, data.typeArguments.start, data.typeArguments.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSignatureNode: {
                    TemplateSignatureNode data = node.As<TemplateSignatureNode>();
                    builder.Append("TemplateSignatureNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.arguments.start, data.arguments.length);
                    VisitExpression(builder, data.companionTypePath.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateConstDeclaration: {
                    TemplateConstDeclaration data = node.As<TemplateConstDeclaration>();
                    builder.Append("TemplateConstDeclaration");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.typePath.id);
                    VisitExpression(builder, data.initializer.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateVariableDeclaration: {
                    TemplateVariableDeclaration data = node.As<TemplateVariableDeclaration>();
                    builder.Append("TemplateVariableDeclaration");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.typePath.id);
                    VisitExpression(builder, data.initializer.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSpawnList: {
                    TemplateSpawnList data = node.As<TemplateSpawnList>();
                    builder.Append("TemplateSpawnList");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.spawnList.start, data.spawnList.length);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateStateDeclaration: {
                    TemplateStateDeclaration data = node.As<TemplateStateDeclaration>();
                    builder.Append("TemplateStateDeclaration");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.typePath.id);
                    VisitExpression(builder, data.initializer.id);
                    VisitImpl(builder, data.fromMapping);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.MethodDeclaration: {
                    MethodDeclaration data = node.As<MethodDeclaration>();
                    builder.Append("MethodDeclaration");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.returnType.id);
                    VisitRange(builder, data.parameters.start, data.parameters.length);
                    VisitExpression(builder, data.body.id);
                    VisitImpl(builder, data.fromMapping);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RepeatParameter: {
                    RepeatParameter data = node.As<RepeatParameter>();
                    builder.Append("RepeatParameter");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.value.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RenderSlotNode: {
                    RenderSlotNode data = node.As<RenderSlotNode>();
                    builder.Append("RenderSlotNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpressionRange(builder, data.parameters.start, data.parameters.length);
                    VisitImpl(builder, data.defaultBlock);
                    VisitExpression(builder, data.dynamicExpression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RenderPortalNode: {
                    RenderPortalNode data = node.As<RenderPortalNode>();
                    builder.Append("RenderPortalNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.RenderMarkerNode: {
                    RenderMarkerNode data = node.As<RenderMarkerNode>();
                    builder.Append("RenderMarkerNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSwitchSection: {
                    TemplateSwitchSection data = node.As<TemplateSwitchSection>();
                    builder.Append("TemplateSwitchSection");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.labels.start, data.labels.length);
                    VisitImpl(builder, data.body);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.TemplateSwitchLabel: {
                    TemplateSwitchLabel data = node.As<TemplateSwitchLabel>();
                    builder.Append("TemplateSwitchLabel");
                    builder.Indent();
                    builder.NewLine();
                    VisitExpression(builder, data.expression.id);
                    builder.Outdent();
                    break;
                }

                case TemplateNodeType.SlotOverrideNode: {
                    SlotOverrideNode data = node.As<SlotOverrideNode>();
                    builder.Append("SlotOverrideNode");
                    builder.Indent();
                    builder.NewLine();
                    VisitRange(builder, data.extrusions.start, data.extrusions.length);
                    VisitImpl(builder, data.contents);
                    VisitExpression(builder, data.slotExpression.id);
                    builder.Outdent();
                    break;
                }
                 
             }
        }
    }
    
    internal partial interface ITemplateVisitor {

        void VisitTemplateBlockNode(in TemplateBlockNode node);

        void VisitTemplateSwitchStatement(in TemplateSwitchStatement node);

        void VisitTemplateIfStatement(in TemplateIfStatement node);

        void VisitStyleListNode(in StyleListNode node);

        void VisitElementNode(in ElementNode node);

        void VisitTemplateElementAssignment(in TemplateElementAssignment node);

        void VisitTemplateSlotSignature(in TemplateSlotSignature node);

        void VisitRepeatNode(in RepeatNode node);

        void VisitTemplateFunctionSignature(in TemplateFunctionSignature node);

        void VisitTemplateFunctionParameter(in TemplateFunctionParameter node);

        void VisitLocalTemplateFn(in LocalTemplateFn node);

        void VisitCreateSection(in CreateSection node);

        void VisitEnableSection(in EnableSection node);

        void VisitRunSection(in RunSection node);

        void VisitMarkerNode(in MarkerNode node);

        void VisitTeleportNode(in TeleportNode node);

        void VisitPropertyBindingNode(in PropertyBindingNode node);

        void VisitDecoratorArgumentNode(in DecoratorArgumentNode node);

        void VisitComputedProperty(in ComputedProperty node);

        void VisitTemplateParameter(in TemplateParameter node);

        void VisitFromMapping(in FromMapping node);

        void VisitOnChangeBindingNode(in OnChangeBindingNode node);

        void VisitLifeCycleEventNode(in LifeCycleEventNode node);

        void VisitInputHandlerNode(in InputHandlerNode node);

        void VisitInstanceStyleNode(in InstanceStyleNode node);

        void VisitAttributeAssignment(in AttributeAssignment node);

        void VisitTypedQualifiedIdentifierNode(in TypedQualifiedIdentifierNode node);

        void VisitExtrusion(in Extrusion node);

        void VisitKeyedArgument(in KeyedArgument node);

        void VisitDecoratorNode(in DecoratorNode node);

        void VisitTemplateSignatureNode(in TemplateSignatureNode node);

        void VisitTemplateConstDeclaration(in TemplateConstDeclaration node);

        void VisitTemplateVariableDeclaration(in TemplateVariableDeclaration node);

        void VisitTemplateSpawnList(in TemplateSpawnList node);

        void VisitTemplateStateDeclaration(in TemplateStateDeclaration node);

        void VisitMethodDeclaration(in MethodDeclaration node);

        void VisitRepeatParameter(in RepeatParameter node);

        void VisitRenderSlotNode(in RenderSlotNode node);

        void VisitRenderPortalNode(in RenderPortalNode node);

        void VisitRenderMarkerNode(in RenderMarkerNode node);

        void VisitTemplateSwitchSection(in TemplateSwitchSection node);

        void VisitTemplateSwitchLabel(in TemplateSwitchLabel node);

        void VisitSlotOverrideNode(in SlotOverrideNode node);
    
    }
    
    internal unsafe partial struct TemplateTree {
            
        partial void VisitImpl(Action<UntypedTemplateNode> action, NodeIndex index) {
            
             if(index.id <= 0) return;
             
             UntypedTemplateNode node = Get(index);
             
             switch (node.meta.nodeType) {

                case TemplateNodeType.TemplateBlockNode: {
                    TemplateBlockNode data = node.As<TemplateBlockNode>();
                    action(node);
                    VisitRange(action, data.statements.start, data.statements.length);
                    break;
                }

                case TemplateNodeType.TemplateSwitchStatement: {
                    TemplateSwitchStatement data = node.As<TemplateSwitchStatement>();
                    action(node);
                    VisitRange(action, data.sections.start, data.sections.length);
                    break;
                }

                case TemplateNodeType.TemplateIfStatement: {
                    TemplateIfStatement data = node.As<TemplateIfStatement>();
                    action(node);
                    VisitImpl(action, data.trueBody);
                    VisitImpl(action, data.falseBody);
                    break;
                }

                case TemplateNodeType.StyleListNode: {
                    StyleListNode data = node.As<StyleListNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.ElementNode: {
                    ElementNode data = node.As<ElementNode>();
                    action(node);
                    VisitRange(action, data.decorators.start, data.decorators.length);
                    VisitRange(action, data.bindings.start, data.bindings.length);
                    VisitRange(action, data.extrusions.start, data.extrusions.length);
                    VisitImpl(action, data.childBlock);
                    break;
                }

                case TemplateNodeType.TemplateElementAssignment: {
                    TemplateElementAssignment data = node.As<TemplateElementAssignment>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateSlotSignature: {
                    TemplateSlotSignature data = node.As<TemplateSlotSignature>();
                    action(node);
                    break;
                }

                case TemplateNodeType.RepeatNode: {
                    RepeatNode data = node.As<RepeatNode>();
                    action(node);
                    VisitRange(action, data.parameters.start, data.parameters.length);
                    VisitRange(action, data.extrusions.start, data.extrusions.length);
                    VisitImpl(action, data.body);
                    break;
                }

                case TemplateNodeType.TemplateFunctionSignature: {
                    TemplateFunctionSignature data = node.As<TemplateFunctionSignature>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateFunctionParameter: {
                    TemplateFunctionParameter data = node.As<TemplateFunctionParameter>();
                    action(node);
                    break;
                }

                case TemplateNodeType.LocalTemplateFn: {
                    LocalTemplateFn data = node.As<LocalTemplateFn>();
                    action(node);
                    VisitRange(action, data.parameters.start, data.parameters.length);
                    VisitImpl(action, data.signature);
                    VisitImpl(action, data.body);
                    break;
                }

                case TemplateNodeType.CreateSection: {
                    CreateSection data = node.As<CreateSection>();
                    action(node);
                    break;
                }

                case TemplateNodeType.EnableSection: {
                    EnableSection data = node.As<EnableSection>();
                    action(node);
                    break;
                }

                case TemplateNodeType.RunSection: {
                    RunSection data = node.As<RunSection>();
                    action(node);
                    break;
                }

                case TemplateNodeType.MarkerNode: {
                    MarkerNode data = node.As<MarkerNode>();
                    action(node);
                    VisitImpl(action, data.block);
                    break;
                }

                case TemplateNodeType.TeleportNode: {
                    TeleportNode data = node.As<TeleportNode>();
                    action(node);
                    VisitImpl(action, data.block);
                    break;
                }

                case TemplateNodeType.PropertyBindingNode: {
                    PropertyBindingNode data = node.As<PropertyBindingNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.DecoratorArgumentNode: {
                    DecoratorArgumentNode data = node.As<DecoratorArgumentNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.ComputedProperty: {
                    ComputedProperty data = node.As<ComputedProperty>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateParameter: {
                    TemplateParameter data = node.As<TemplateParameter>();
                    action(node);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.FromMapping: {
                    FromMapping data = node.As<FromMapping>();
                    action(node);
                    break;
                }

                case TemplateNodeType.OnChangeBindingNode: {
                    OnChangeBindingNode data = node.As<OnChangeBindingNode>();
                    action(node);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.LifeCycleEventNode: {
                    LifeCycleEventNode data = node.As<LifeCycleEventNode>();
                    action(node);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.InputHandlerNode: {
                    InputHandlerNode data = node.As<InputHandlerNode>();
                    action(node);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.InstanceStyleNode: {
                    InstanceStyleNode data = node.As<InstanceStyleNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.AttributeAssignment: {
                    AttributeAssignment data = node.As<AttributeAssignment>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TypedQualifiedIdentifierNode: {
                    TypedQualifiedIdentifierNode data = node.As<TypedQualifiedIdentifierNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.Extrusion: {
                    Extrusion data = node.As<Extrusion>();
                    action(node);
                    break;
                }

                case TemplateNodeType.KeyedArgument: {
                    KeyedArgument data = node.As<KeyedArgument>();
                    action(node);
                    break;
                }

                case TemplateNodeType.DecoratorNode: {
                    DecoratorNode data = node.As<DecoratorNode>();
                    action(node);
                    VisitRange(action, data.arguments.start, data.arguments.length);
                    VisitRange(action, data.extrusions.start, data.extrusions.length);
                    break;
                }

                case TemplateNodeType.TemplateSignatureNode: {
                    TemplateSignatureNode data = node.As<TemplateSignatureNode>();
                    action(node);
                    VisitRange(action, data.arguments.start, data.arguments.length);
                    break;
                }

                case TemplateNodeType.TemplateConstDeclaration: {
                    TemplateConstDeclaration data = node.As<TemplateConstDeclaration>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateVariableDeclaration: {
                    TemplateVariableDeclaration data = node.As<TemplateVariableDeclaration>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateSpawnList: {
                    TemplateSpawnList data = node.As<TemplateSpawnList>();
                    action(node);
                    VisitRange(action, data.spawnList.start, data.spawnList.length);
                    break;
                }

                case TemplateNodeType.TemplateStateDeclaration: {
                    TemplateStateDeclaration data = node.As<TemplateStateDeclaration>();
                    action(node);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.MethodDeclaration: {
                    MethodDeclaration data = node.As<MethodDeclaration>();
                    action(node);
                    VisitRange(action, data.parameters.start, data.parameters.length);
                    VisitImpl(action, data.fromMapping);
                    break;
                }

                case TemplateNodeType.RepeatParameter: {
                    RepeatParameter data = node.As<RepeatParameter>();
                    action(node);
                    break;
                }

                case TemplateNodeType.RenderSlotNode: {
                    RenderSlotNode data = node.As<RenderSlotNode>();
                    action(node);
                    VisitImpl(action, data.defaultBlock);
                    break;
                }

                case TemplateNodeType.RenderPortalNode: {
                    RenderPortalNode data = node.As<RenderPortalNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.RenderMarkerNode: {
                    RenderMarkerNode data = node.As<RenderMarkerNode>();
                    action(node);
                    break;
                }

                case TemplateNodeType.TemplateSwitchSection: {
                    TemplateSwitchSection data = node.As<TemplateSwitchSection>();
                    action(node);
                    VisitRange(action, data.labels.start, data.labels.length);
                    VisitImpl(action, data.body);
                    break;
                }

                case TemplateNodeType.TemplateSwitchLabel: {
                    TemplateSwitchLabel data = node.As<TemplateSwitchLabel>();
                    action(node);
                    break;
                }

                case TemplateNodeType.SlotOverrideNode: {
                    SlotOverrideNode data = node.As<SlotOverrideNode>();
                    action(node);
                    VisitRange(action, data.extrusions.start, data.extrusions.length);
                    VisitImpl(action, data.contents);
                    break;
                }
                 
             }
        
        }
        
        partial void InterfaceVisitImpl<T>(ref T visitor, NodeIndex index) where T : ITemplateVisitor {
                    
            if(index.id <= 0) return;
            
            UntypedTemplateNode * node = untypedNodes.GetPointer(index.id);
            
            switch (node->meta.nodeType) {

                case TemplateNodeType.TemplateBlockNode: {
                    TemplateBlockNode data = node->As<TemplateBlockNode>();
                    visitor.VisitTemplateBlockNode(data);
                    break;
                }

                case TemplateNodeType.TemplateSwitchStatement: {
                    TemplateSwitchStatement data = node->As<TemplateSwitchStatement>();
                    visitor.VisitTemplateSwitchStatement(data);
                    break;
                }

                case TemplateNodeType.TemplateIfStatement: {
                    TemplateIfStatement data = node->As<TemplateIfStatement>();
                    visitor.VisitTemplateIfStatement(data);
                    break;
                }

                case TemplateNodeType.StyleListNode: {
                    StyleListNode data = node->As<StyleListNode>();
                    visitor.VisitStyleListNode(data);
                    break;
                }

                case TemplateNodeType.ElementNode: {
                    ElementNode data = node->As<ElementNode>();
                    visitor.VisitElementNode(data);
                    break;
                }

                case TemplateNodeType.TemplateElementAssignment: {
                    TemplateElementAssignment data = node->As<TemplateElementAssignment>();
                    visitor.VisitTemplateElementAssignment(data);
                    break;
                }

                case TemplateNodeType.TemplateSlotSignature: {
                    TemplateSlotSignature data = node->As<TemplateSlotSignature>();
                    visitor.VisitTemplateSlotSignature(data);
                    break;
                }

                case TemplateNodeType.RepeatNode: {
                    RepeatNode data = node->As<RepeatNode>();
                    visitor.VisitRepeatNode(data);
                    break;
                }

                case TemplateNodeType.TemplateFunctionSignature: {
                    TemplateFunctionSignature data = node->As<TemplateFunctionSignature>();
                    visitor.VisitTemplateFunctionSignature(data);
                    break;
                }

                case TemplateNodeType.TemplateFunctionParameter: {
                    TemplateFunctionParameter data = node->As<TemplateFunctionParameter>();
                    visitor.VisitTemplateFunctionParameter(data);
                    break;
                }

                case TemplateNodeType.LocalTemplateFn: {
                    LocalTemplateFn data = node->As<LocalTemplateFn>();
                    visitor.VisitLocalTemplateFn(data);
                    break;
                }

                case TemplateNodeType.CreateSection: {
                    CreateSection data = node->As<CreateSection>();
                    visitor.VisitCreateSection(data);
                    break;
                }

                case TemplateNodeType.EnableSection: {
                    EnableSection data = node->As<EnableSection>();
                    visitor.VisitEnableSection(data);
                    break;
                }

                case TemplateNodeType.RunSection: {
                    RunSection data = node->As<RunSection>();
                    visitor.VisitRunSection(data);
                    break;
                }

                case TemplateNodeType.MarkerNode: {
                    MarkerNode data = node->As<MarkerNode>();
                    visitor.VisitMarkerNode(data);
                    break;
                }

                case TemplateNodeType.TeleportNode: {
                    TeleportNode data = node->As<TeleportNode>();
                    visitor.VisitTeleportNode(data);
                    break;
                }

                case TemplateNodeType.PropertyBindingNode: {
                    PropertyBindingNode data = node->As<PropertyBindingNode>();
                    visitor.VisitPropertyBindingNode(data);
                    break;
                }

                case TemplateNodeType.DecoratorArgumentNode: {
                    DecoratorArgumentNode data = node->As<DecoratorArgumentNode>();
                    visitor.VisitDecoratorArgumentNode(data);
                    break;
                }

                case TemplateNodeType.ComputedProperty: {
                    ComputedProperty data = node->As<ComputedProperty>();
                    visitor.VisitComputedProperty(data);
                    break;
                }

                case TemplateNodeType.TemplateParameter: {
                    TemplateParameter data = node->As<TemplateParameter>();
                    visitor.VisitTemplateParameter(data);
                    break;
                }

                case TemplateNodeType.FromMapping: {
                    FromMapping data = node->As<FromMapping>();
                    visitor.VisitFromMapping(data);
                    break;
                }

                case TemplateNodeType.OnChangeBindingNode: {
                    OnChangeBindingNode data = node->As<OnChangeBindingNode>();
                    visitor.VisitOnChangeBindingNode(data);
                    break;
                }

                case TemplateNodeType.LifeCycleEventNode: {
                    LifeCycleEventNode data = node->As<LifeCycleEventNode>();
                    visitor.VisitLifeCycleEventNode(data);
                    break;
                }

                case TemplateNodeType.InputHandlerNode: {
                    InputHandlerNode data = node->As<InputHandlerNode>();
                    visitor.VisitInputHandlerNode(data);
                    break;
                }

                case TemplateNodeType.InstanceStyleNode: {
                    InstanceStyleNode data = node->As<InstanceStyleNode>();
                    visitor.VisitInstanceStyleNode(data);
                    break;
                }

                case TemplateNodeType.AttributeAssignment: {
                    AttributeAssignment data = node->As<AttributeAssignment>();
                    visitor.VisitAttributeAssignment(data);
                    break;
                }

                case TemplateNodeType.TypedQualifiedIdentifierNode: {
                    TypedQualifiedIdentifierNode data = node->As<TypedQualifiedIdentifierNode>();
                    visitor.VisitTypedQualifiedIdentifierNode(data);
                    break;
                }

                case TemplateNodeType.Extrusion: {
                    Extrusion data = node->As<Extrusion>();
                    visitor.VisitExtrusion(data);
                    break;
                }

                case TemplateNodeType.KeyedArgument: {
                    KeyedArgument data = node->As<KeyedArgument>();
                    visitor.VisitKeyedArgument(data);
                    break;
                }

                case TemplateNodeType.DecoratorNode: {
                    DecoratorNode data = node->As<DecoratorNode>();
                    visitor.VisitDecoratorNode(data);
                    break;
                }

                case TemplateNodeType.TemplateSignatureNode: {
                    TemplateSignatureNode data = node->As<TemplateSignatureNode>();
                    visitor.VisitTemplateSignatureNode(data);
                    break;
                }

                case TemplateNodeType.TemplateConstDeclaration: {
                    TemplateConstDeclaration data = node->As<TemplateConstDeclaration>();
                    visitor.VisitTemplateConstDeclaration(data);
                    break;
                }

                case TemplateNodeType.TemplateVariableDeclaration: {
                    TemplateVariableDeclaration data = node->As<TemplateVariableDeclaration>();
                    visitor.VisitTemplateVariableDeclaration(data);
                    break;
                }

                case TemplateNodeType.TemplateSpawnList: {
                    TemplateSpawnList data = node->As<TemplateSpawnList>();
                    visitor.VisitTemplateSpawnList(data);
                    break;
                }

                case TemplateNodeType.TemplateStateDeclaration: {
                    TemplateStateDeclaration data = node->As<TemplateStateDeclaration>();
                    visitor.VisitTemplateStateDeclaration(data);
                    break;
                }

                case TemplateNodeType.MethodDeclaration: {
                    MethodDeclaration data = node->As<MethodDeclaration>();
                    visitor.VisitMethodDeclaration(data);
                    break;
                }

                case TemplateNodeType.RepeatParameter: {
                    RepeatParameter data = node->As<RepeatParameter>();
                    visitor.VisitRepeatParameter(data);
                    break;
                }

                case TemplateNodeType.RenderSlotNode: {
                    RenderSlotNode data = node->As<RenderSlotNode>();
                    visitor.VisitRenderSlotNode(data);
                    break;
                }

                case TemplateNodeType.RenderPortalNode: {
                    RenderPortalNode data = node->As<RenderPortalNode>();
                    visitor.VisitRenderPortalNode(data);
                    break;
                }

                case TemplateNodeType.RenderMarkerNode: {
                    RenderMarkerNode data = node->As<RenderMarkerNode>();
                    visitor.VisitRenderMarkerNode(data);
                    break;
                }

                case TemplateNodeType.TemplateSwitchSection: {
                    TemplateSwitchSection data = node->As<TemplateSwitchSection>();
                    visitor.VisitTemplateSwitchSection(data);
                    break;
                }

                case TemplateNodeType.TemplateSwitchLabel: {
                    TemplateSwitchLabel data = node->As<TemplateSwitchLabel>();
                    visitor.VisitTemplateSwitchLabel(data);
                    break;
                }

                case TemplateNodeType.SlotOverrideNode: {
                    SlotOverrideNode data = node->As<SlotOverrideNode>();
                    visitor.VisitSlotOverrideNode(data);
                    break;
                }
                 
            }
                
        }
        
    }
        
}