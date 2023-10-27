using System;

namespace EvolveUI.Parsing {

    internal static unsafe class StructureVisit {

        public static void Visit<T>(ref T visitor, TemplateFile* templateFile, NodeRange nodeRange) where T : ITemplateStructureVisitor {
            if (!nodeRange.IsValid) return;
            for (int i = 0; i < nodeRange.length; i++) {
                Visit(ref visitor, templateFile, templateFile->templateTree->ranges[nodeRange.start + i]);
            }
        }

        public static void Visit<T>(T visitor, TemplateFile* templateFile, NodeRange nodeRange) where T : ITemplateStructureVisitor {
            if (!nodeRange.IsValid) return;
            for (int i = 0; i < nodeRange.length; i++) {
                Visit(ref visitor, templateFile, templateFile->templateTree->ranges[nodeRange.start + i]);
            }
        }

        public static void Visit<T>(T visitor, TemplateFile* templateFile, NodeIndex nodeIndex) where T : ITemplateStructureVisitor {
            if (!nodeIndex.IsValid) return;

            UntypedTemplateNode statement = templateFile->Get(nodeIndex);
            switch (statement.meta.GetStructureType()) {
                case TemplateStructureType.TemplateBlockNode: {
                    visitor.VisitBlockNode(statement.As<TemplateBlockNode>());
                    break;
                }

                case TemplateStructureType.TemplateIfStatement: {
                    visitor.VisitIfStatementNode(statement.As<TemplateIfStatement>());
                    break;
                }

                case TemplateStructureType.ElementNode: {
                    visitor.VisitElementNode(statement.As<ElementNode>());
                    break;
                }

                case TemplateStructureType.RepeatNode: {
                    visitor.VisitRepeatNode(statement.As<RepeatNode>());
                    break;
                }

                case TemplateStructureType.RunSection: {
                    visitor.VisitRunStatement(statement.As<RunSection>());
                    break;
                }

                case TemplateStructureType.CreateSection: {
                    visitor.VisitCreateSectionNode(statement.As<CreateSection>());
                    break;
                }

                case TemplateStructureType.EnableSection: {
                    visitor.VisitEnableSectionNode(statement.As<EnableSection>());
                    break;
                }

                case TemplateStructureType.MarkerNode: {
                    visitor.VisitMarkerNode(statement.As<MarkerNode>());
                    break;
                }

                case TemplateStructureType.TemplateConstDeclaration: {
                    visitor.VisitConstDeclaration(statement.As<TemplateConstDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateVariableDeclaration: {
                    visitor.VisitVariableDeclaration(statement.As<TemplateVariableDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateStateDeclaration: {
                    visitor.VisitStateDeclaration(statement.As<TemplateStateDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateSwitchStatement: {
                    visitor.VisitSwitchStatement(statement.As<TemplateSwitchStatement>());
                    break;
                }

                case TemplateStructureType.LocalTemplateFn: {
                    visitor.VisitLocalTemplateFn(statement.As<LocalTemplateFn>());
                    break;
                }

                case TemplateStructureType.LocalMethodDeclaration: {
                    visitor.VisitLocalMethodDeclaration(statement.As<MethodDeclaration>());
                    break;
                }

                case TemplateStructureType.RenderSlotNode: {
                    visitor.VisitRenderSlotNode(statement.As<RenderSlotNode>());
                    break;
                }

                case TemplateStructureType.RenderPortalNode: {
                    visitor.VisitRenderPortalNode(statement.As<RenderPortalNode>());
                    break;
                }

                case TemplateStructureType.RenderMarkerNode: {
                    visitor.VisitRenderMarkerNode(statement.As<RenderMarkerNode>());
                    break;
                }

                case TemplateStructureType.None: {
                    return;
                }
                
                case TemplateStructureType.SlotOverrideNode: {
                    throw new ArgumentOutOfRangeException($"Slot override must be defined before implicit content.");
                }

                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static void Visit<T>(ref T visitor, TemplateFile* templateFile, NodeIndex nodeIndex) where T : ITemplateStructureVisitor {
            if (!nodeIndex.IsValid) return;

            UntypedTemplateNode statement = templateFile->Get(nodeIndex);
            switch (statement.meta.GetStructureType()) {
                case TemplateStructureType.TemplateBlockNode: {
                    visitor.VisitBlockNode(statement.As<TemplateBlockNode>());
                    break;
                }

                case TemplateStructureType.TemplateIfStatement: {
                    visitor.VisitIfStatementNode(statement.As<TemplateIfStatement>());
                    break;
                }

                case TemplateStructureType.ElementNode: {
                    visitor.VisitElementNode(statement.As<ElementNode>());
                    break;
                }

                case TemplateStructureType.RepeatNode: {
                    visitor.VisitRepeatNode(statement.As<RepeatNode>());
                    break;
                }

                case TemplateStructureType.RunSection: {
                    visitor.VisitRunStatement(statement.As<RunSection>());
                    break;
                }

                case TemplateStructureType.CreateSection: {
                    visitor.VisitCreateSectionNode(statement.As<CreateSection>());
                    break;
                }

                case TemplateStructureType.EnableSection: {
                    visitor.VisitEnableSectionNode(statement.As<EnableSection>());
                    break;
                }

                case TemplateStructureType.MarkerNode: {
                    visitor.VisitMarkerNode(statement.As<MarkerNode>());
                    break;
                }

                case TemplateStructureType.TemplateConstDeclaration: {
                    visitor.VisitConstDeclaration(statement.As<TemplateConstDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateVariableDeclaration: {
                    visitor.VisitVariableDeclaration(statement.As<TemplateVariableDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateStateDeclaration: {
                    visitor.VisitStateDeclaration(statement.As<TemplateStateDeclaration>());
                    break;
                }

                case TemplateStructureType.TemplateSwitchStatement: {
                    visitor.VisitSwitchStatement(statement.As<TemplateSwitchStatement>());
                    break;
                }

                case TemplateStructureType.LocalTemplateFn: {
                    visitor.VisitLocalTemplateFn(statement.As<LocalTemplateFn>());
                    break;
                }

                case TemplateStructureType.LocalMethodDeclaration: {
                    visitor.VisitLocalMethodDeclaration(statement.As<MethodDeclaration>());
                    break;
                }

                case TemplateStructureType.RenderSlotNode: {
                    visitor.VisitRenderSlotNode(statement.As<RenderSlotNode>());
                    break;
                }

                case TemplateStructureType.RenderPortalNode: {
                    visitor.VisitRenderPortalNode(statement.As<RenderPortalNode>());
                    break;
                }

                case TemplateStructureType.RenderMarkerNode: {
                    visitor.VisitRenderMarkerNode(statement.As<RenderMarkerNode>());
                    break;
                }

                case TemplateStructureType.SlotOverrideNode: {
                    visitor.VisitSlotOverrideNode(statement.As<SlotOverrideNode>());
                    break;
                }

                case TemplateStructureType.TeleportNode: {
                    visitor.VisitTeleportNode(statement.As<TeleportNode>());
                    break;
                }

                case TemplateStructureType.TemplateSwitchSection: {
                    visitor.VisitSwitchSection(statement.As<TemplateSwitchSection>());
                    return;
                }

                case TemplateStructureType.None: {
                    return;
                }

                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

    }

    internal partial interface ITemplateStructureVisitor {

        void VisitElementNode(in ElementNode elementNode);

        void VisitIfStatementNode(in TemplateIfStatement node);

        void VisitSlotOverrideNode(SlotOverrideNode node);

        void VisitRenderMarkerNode(RenderMarkerNode node);

        void VisitRenderPortalNode(RenderPortalNode node);

        void VisitRenderSlotNode(RenderSlotNode node);

        void VisitLocalMethodDeclaration(MethodDeclaration node);

        void VisitLocalTemplateFn(LocalTemplateFn node);

        void VisitSwitchStatement(TemplateSwitchStatement node);

        void VisitStateDeclaration(TemplateStateDeclaration node);

        void VisitVariableDeclaration(TemplateVariableDeclaration node);

        void VisitConstDeclaration(TemplateConstDeclaration node);

        void VisitMarkerNode(MarkerNode node);

        void VisitEnableSectionNode(EnableSection node);

        void VisitCreateSectionNode(CreateSection node);

        void VisitTeleportNode(TeleportNode node);

        void VisitRunStatement(RunSection node);

        void VisitBlockNode(TemplateBlockNode node);

        void VisitRepeatNode(in RepeatNode node);
        
        void VisitSwitchSection(in TemplateSwitchSection node);
        
    }

}