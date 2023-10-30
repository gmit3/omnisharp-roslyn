using System;
using System.Diagnostics;
using EvolveUI;
using EvolveUI.Parsing;
using EvolveUI.Util;

namespace OmniSharp.Roslyn
{
    internal partial class EvolveUIMapper
    {
        private readonly string processed_marker = "<<EvolveUI processed marker>>";
        private void ProcessSource()
        {
            try
            {
                Debug.Assert(!original_string.Contains(processed_marker));

                if(!TemplateParser.TryParseTemplate(filepath, original_string, out TemplateParseResult result))
                    return;

                unsafe
                {
                    fixed(NodeIndex* templateRanges = result.templateRanges)
                    fixed(UntypedTemplateNode* templateNodes = result.templateNodes)
                    fixed(UntypedExpressionNode* expressionNodes = result.expressionNodes)
                    fixed(ExpressionIndex* expressionRanges = result.expressionRanges)
                    fixed(Token* nonTrivialTokens = result.nonTrivialTokens)
                    fixed(char* pSource = original_string)
                    {
                        ExpressionTree expressionTree = new ExpressionTree() {
                            nonTrivialTokens = new CheckedArray<Token>(nonTrivialTokens, result.nonTrivialTokens.Length),
                            untypedNodes = new CheckedArray<UntypedExpressionNode>(expressionNodes, result.expressionNodes.Length),
                            ranges = new CheckedArray<ExpressionIndex>(expressionRanges, result.expressionRanges.Length),
                            source = new FixedCharacterSpan(pSource, original_string.Length),
                        };

                        TemplateTree templateTree = new TemplateTree() {
                            nonTrivialTokens = new CheckedArray<Token>(nonTrivialTokens, result.nonTrivialTokens.Length),
                            untypedNodes = new CheckedArray<UntypedTemplateNode>(templateNodes, result.templateNodes.Length),
                            ranges = new CheckedArray<NodeIndex>(templateRanges, result.templateRanges.Length),
                            source = new FixedCharacterSpan(pSource, original_string.Length),
                            expressionTree = &expressionTree
                        };

                        //                    TemplatePrintingVisitor visitor = new TemplatePrintingVisitor(&templateTree, printExpressions);

                        void DoTemplateNode(UntypedTemplateNode p)
                        {
                            switch(p.meta.nodeType)
                            {
                            case TemplateNodeType.TemplateSpawnList:
                                var node = p.As<TemplateSpawnList>();
                                break;
                            }
                        }
                        void DoExpressionNode(UntypedExpressionNode p)
                        {
                        }

                        string GetTokensString(int i, int j)
                        {
                            Debug.Assert(i <= j);
                            return original_string.Substring(result.nonTrivialTokens[i].charIndex,
                                result.nonTrivialTokens[j].charIndex + result.nonTrivialTokens[j].length - result.nonTrivialTokens[i].charIndex);

                        }
                        string GetTokenString(int i)
                        {
                            return GetTokensString(i, i);
                        }

                        (int, int) GetTokensIndices(int i, int j)
                        {
                            Debug.Assert(i <= j);
                            return (result.nonTrivialTokens[i].charIndex,
                                result.nonTrivialTokens[j].charIndex + result.nonTrivialTokens[j].length);
                        }
                        (int, int) GetTokenIndices(int i)
                        {
                            return GetTokensIndices(i, i);
                        }

                        TopLevelDeclaration[] topLevel = result.topLevelDeclarations;
                        for(int i = 0; i < topLevel.Length; i++)
                        {
                            ref var declaration = ref topLevel[i];
                            switch(declaration.type)
                            {
                            case DeclarationType.Invalid:
                                break;

                            case DeclarationType.Using:
                                UsingDeclaration usingDeclaration = declaration.usingDeclaration;
                                expressionTree.Visit(usingDeclaration.typePath, DoExpressionNode);
                                break;

                            case DeclarationType.Import:
                                ImportDeclaration importDeclaration = declaration.importDeclaration;
                                break;

                            case DeclarationType.Template:
                                TemplateDeclaration templateDecl = declaration.templateDeclaration;

                                // bool is_render = false; //#EVOLVEUI todo
                                Replace(GetTokensIndices(templateDecl.tokenRange.start.index,
                                    templateDecl.identifierLocation.index), $"class __EvolveUI_template_{GetTokenString(templateDecl.identifierLocation.index)}");

                                templateTree.VisitRange(DoTemplateNode, templateDecl.decorators.start, templateDecl.decorators.length);
                                templateTree.Visit(templateDecl.signature, DoTemplateNode);
                                templateTree.Visit(templateDecl.spawnList, DoTemplateNode);
                                templateTree.Visit(templateDecl.body, DoTemplateNode);
                                break;

                            case DeclarationType.Typography:
                                TypographyDeclaration typography = declaration.typographyDeclaration;
                                templateTree.VisitRange(DoTemplateNode, typography.decorators.start, typography.decorators.length);
                                templateTree.Visit(typography.signature, DoTemplateNode);
                                templateTree.Visit(typography.spawnList, DoTemplateNode);
                                break;

                            case DeclarationType.RootFunction:
                                break;

                            case DeclarationType.Decorator:
                                DecoratorDeclaration decoratorDeclaration = declaration.decoratorDeclaration;
                                templateTree.VisitRange(DoTemplateNode, decoratorDeclaration.typeParameters.start, decoratorDeclaration.typeParameters.length);
                                templateTree.Visit(decoratorDeclaration.signature, DoTemplateNode);
                                templateTree.Visit(decoratorDeclaration.spawnList, DoTemplateNode);
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // #TODO: inserting the same single point won't work yet
            InsertLine(0, $"// {processed_marker} blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            //             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            //             InsertLine(0, "// blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip blip");
            //             Replace("template AppRoot : AppRoot", "class __evolveUI__AppRoot");
            //             ReplaceAll("state", "");
            //             ReplaceAll("[@", "\"xx-style-xx\"", "]");
        }
    }
}
