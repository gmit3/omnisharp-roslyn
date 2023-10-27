using EvolveUI.Util;

namespace EvolveUI.Parsing {

    internal unsafe ref partial struct TemplatePrintingVisitor {

        private readonly TemplateTree* tree;
        private readonly bool printExpressions;

        private TemplatePrintingVisitor(TemplateTree* templateTree, bool printExpressions = false) {
            this.tree = templateTree;
            this.printExpressions = printExpressions;
        }

        public static string Print(string source, TemplateParseResult result, bool printExpressions = true) {
            IndentedStringBuilder builder = new IndentedStringBuilder(1024);

            if (result.errors.Length != 0) {
                for (int i = 0; i < result.errors.Length; i++) {
                    builder.Append(result.errors[i].message);
                }
            }
            else {
                fixed (NodeIndex* templateRanges = result.templateRanges)
                fixed (UntypedTemplateNode* templateNodes = result.templateNodes)
                fixed (UntypedExpressionNode* expressionNodes = result.expressionNodes)
                fixed (ExpressionIndex* expressionRanges = result.expressionRanges)
                fixed (Token* nonTrivialTokens = result.nonTrivialTokens)
                fixed (char* pSource = source) {
                    ExpressionTree expressionTree = new ExpressionTree() {
                        nonTrivialTokens = new CheckedArray<Token>(nonTrivialTokens, result.nonTrivialTokens.Length),
                        untypedNodes = new CheckedArray<UntypedExpressionNode>(expressionNodes, result.expressionNodes.Length),
                        ranges = new CheckedArray<ExpressionIndex>(expressionRanges, result.expressionRanges.Length),
                        source = new FixedCharacterSpan(pSource, source.Length),
                    };
                    
                    TemplateTree templateTree = new TemplateTree() {
                        nonTrivialTokens = new CheckedArray<Token>(nonTrivialTokens, result.nonTrivialTokens.Length),
                        untypedNodes = new CheckedArray<UntypedTemplateNode>(templateNodes, result.templateNodes.Length),
                        ranges = new CheckedArray<NodeIndex>(templateRanges, result.templateRanges.Length),
                        source = new FixedCharacterSpan(pSource, source.Length),
                        expressionTree = &expressionTree
                    };

                    TemplatePrintingVisitor visitor = new TemplatePrintingVisitor(&templateTree, printExpressions);
                    
                    TopLevelDeclaration[] topLevel = result.topLevelDeclarations;
                    
                    for (int i = 0; i < topLevel.Length; i++) {
                        visitor.Visit(builder, topLevel[i]);
                    }

                }

            }

            return builder.ToString();
        }

        public static string Print(NodeIndex nodeIndex, TemplateTree* tree, bool printExpressions = false) {
            IndentedStringBuilder builder = new IndentedStringBuilder(1024);
            new TemplatePrintingVisitor(tree, printExpressions).Visit(builder, nodeIndex);
            return builder.ToString();
        }

        private void Visit(IndentedStringBuilder builder, TopLevelDeclaration declaration) {
            switch (declaration.type) {
                case DeclarationType.Invalid:
                    break;

                case DeclarationType.Using: {
                    UsingDeclaration usingDeclaration = declaration.usingDeclaration;
                    builder.Append("Using");
                    builder.NewLine();
                    builder.Indent();
                    VisitExpression(builder, usingDeclaration.typePath.id);
                    builder.Outdent();
                    break;
                }

                case DeclarationType.Import:
                    ImportDeclaration importDeclaration = declaration.importDeclaration;
                    builder.Append("Import ");
                    builder.Append(tree->GetTokenSource(importDeclaration.importString).ToString());
                    builder.NewLine();
                    
                    break;

                case DeclarationType.Template: {
                    TemplateDeclaration templateDecl = declaration.templateDeclaration;
                    builder.Append("Template ");
                    builder.Append(tree->GetTokenSource(templateDecl.identifierLocation).ToString());
                    builder.NewLine();
                    builder.Indent();
                    VisitRange(builder, templateDecl.decorators.start, templateDecl.decorators.length);
                    Visit(builder, templateDecl.signature);
                    Visit(builder, templateDecl.spawnList);
                    Visit(builder, templateDecl.body);
                    builder.Outdent();
                    break;
                }

                case DeclarationType.Typography:
                    TypographyDeclaration typography = declaration.typographyDeclaration;
                    builder.Append("Typography ");
                    builder.Append(tree->GetTokenSource(typography.identifierLocation).ToString());
                    builder.NewLine();
                    builder.Indent();
                    VisitRange(builder, typography.decorators.start, typography.decorators.length);
                    Visit(builder, typography.signature);
                    Visit(builder, typography.spawnList);
                    builder.Outdent();
                    break;

                case DeclarationType.RootFunction:
                    break;

                case DeclarationType.Decorator:
                    DecoratorDeclaration decoratorDeclaration = declaration.decoratorDeclaration;
                    builder.Append("Decorator ");
                    builder.Append(tree->GetTokenSource(decoratorDeclaration.identifierLocation).ToString());
                    VisitRange(builder, decoratorDeclaration.typeParameters.start, decoratorDeclaration.typeParameters.length);
                    builder.NewLine();
                    builder.Indent();
                    Visit(builder, decoratorDeclaration.signature);
                    Visit(builder, decoratorDeclaration.spawnList);
                    builder.Outdent();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Visit(IndentedStringBuilder builder, NodeIndex nodeIndex) {
            VisitImpl(builder, nodeIndex);
        }

        partial void VisitImpl(IndentedStringBuilder builder, NodeIndex index);

        private void VisitRange(IndentedStringBuilder builder, int start, int length) {
            for (int i = start; i < start + length; i++) {
                VisitImpl(builder, tree->ranges[i]);
            }
        }

        private void VisitExpression(IndentedStringBuilder builder, int id) {
            if (!printExpressions) {
                return;
            }

            // builder.Indent();
            ExpressionPrintingVisitor.Print(builder, new ExpressionIndex(id), tree->expressionTree);
            // builder.Outdent();
        }

        private void VisitExpressionRange(IndentedStringBuilder builder, int start, int length) {
            if (!printExpressions) {
                return;
            }

            for (int i = start; i < start + length; i++) {
                VisitExpression(builder, tree->expressionTree->ranges[i].id);
            }
        }

    }

}