using System.Diagnostics;
using EvolveUI.Native;
using EvolveUI.Parsing;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Testing {

    public struct ParserData : IDisposable {

        public PodList<char> source;
        public PodList<Token> inputTokens;
        public PodList<Token> nonTrivialTokens;
        public PodList<UntypedExpressionNode> expressions;
        public PodList<UntypedTemplateNode> templateNodes;
        public PodList<NodeIndex> templateRanges;
        public PodList<ExpressionIndex> expressionRanges;
        public ExpressionTree expressionTree;
        public TemplateTree templateTree;

        public void Dispose() {
            templateNodes.Dispose();
            templateRanges.Dispose();
            source.Dispose();
            inputTokens.Dispose();
            nonTrivialTokens.Dispose();
            expressions.Dispose();
            expressionRanges.Dispose();
        }

    }

    public unsafe class TestParser : IDisposable {

        private List<IntPtr> parses = new List<IntPtr>();

        public bool ParseTemplate(string input, out TemplateDeclaration resultIndex, out TemplateTree* templateTree) {
            using TemplateParser parser = TemplateParser.Create();
            ParserData* parserData = CreateParseData(input, parser.panic, out TokenStream tokenStream);

            bool result = parser.ParseTemplateDeclaration(ref tokenStream, 0, 0, default, out resultIndex);

            if (tokenStream.HasMoreTokens) {
                result = false;
            }

            List<HardErrorInfo> errorInfos = new List<HardErrorInfo>();

            if (parser.GetHardErrors(errorInfos)) {
                for (int i = 0; i < errorInfos.Count; i++) {
                    LogUtil.Log(TemplateParser.TranslateError(
                        errorInfos[i],
                        parserData->inputTokens.ToCheckedArray(),
                        parserData->nonTrivialTokens.ToCheckedArray(),
                        parserData->source.ToCheckedArray())
                    );
                }

                result = false;
            }

            if (result) {
                parser.GetTemplateBufferCounts(out int templateNodeCount, out int templateRangeCount);
                parser.GetExpressionBufferCounts(out int expressionCount, out int expressionRangeCount);

                parserData->templateNodes = new PodList<UntypedTemplateNode>(templateNodeCount);
                parserData->templateRanges = new PodList<NodeIndex>(templateRangeCount);
                parserData->expressions = new PodList<UntypedExpressionNode>(expressionCount);
                parserData->expressionRanges = new PodList<ExpressionIndex>(expressionRangeCount);

                parser.CopyTemplateNodes(&parserData->templateNodes, &parserData->templateRanges);
                parser.CopyExpressionNodes(&parserData->expressions, &parserData->expressionRanges);

                parserData->expressionTree = new ExpressionTree() {
                    ranges = parserData->expressionRanges.ToCheckedArray(),
                    source = new FixedCharacterSpan(parserData->source.ToCheckedArray()),
                    untypedNodes = parserData->expressions.ToCheckedArray(),
                    nonTrivialTokens = parserData->nonTrivialTokens.ToCheckedArray()
                };

                parserData->templateTree = new TemplateTree() {
                    ranges = parserData->templateRanges.ToCheckedArray(),
                    source = new FixedCharacterSpan(parserData->source.GetArrayPointer(), parserData->source.size),
                    untypedNodes = parserData->templateNodes.ToCheckedArray(),
                    nonTrivialTokens = parserData->nonTrivialTokens.ToCheckedArray(),
                    expressionTree = &parserData->expressionTree
                };

                templateTree = &parserData->templateTree;

                return true;
            }

            templateTree = default;
            return false;
        }

        private static bool ProcessExpressionResults(out ExpressionTree* expressionTree, TokenStream tokenStream, bool result, TemplateParser parser, ParserData* parserData) {
            List<HardErrorInfo> errorInfos = new List<HardErrorInfo>();
            if (parser.GetHardErrors(errorInfos)) {
                for (int i = 0; i < errorInfos.Count; i++) {
                    LogUtil.Log(TemplateParser.TranslateError(
                        errorInfos[i],
                        parserData->inputTokens.ToCheckedArray(),
                        parserData->nonTrivialTokens.ToCheckedArray(),
                        parserData->source.ToCheckedArray())
                    );
                }

                result = false;
            }

            if (result && !tokenStream.HasMoreTokens) {

                parser.GetExpressionBufferCounts(out int expressionCount, out int expressionRangeCount);

                parserData->expressions = new PodList<UntypedExpressionNode>(expressionCount);
                parserData->expressionRanges = new PodList<ExpressionIndex>(expressionRangeCount);

                parser.CopyExpressionNodes(&parserData->expressions, &parserData->expressionRanges);

                parserData->expressionTree = new ExpressionTree() {
                    ranges = parserData->expressionRanges.ToCheckedArray(),
                    source = new FixedCharacterSpan(parserData->source.ToCheckedArray()),
                    untypedNodes = parserData->expressions.ToCheckedArray(),
                    nonTrivialTokens = parserData->nonTrivialTokens.ToCheckedArray()
                };

                expressionTree = &parserData->expressionTree;
                return true;
            }

            expressionTree = default;
            return false;
        }

        private bool ParseStatements(string input, out ExpressionRange expressionRange, out ExpressionTree* expressionTree) {
            using TemplateParser parser = TemplateParser.Create();
            ParserData* parserData = CreateParseData(input, parser.panic, out TokenStream tokenStream);
            bool result = parser.ParseCSStatements(ref tokenStream, out expressionRange);
            return ProcessExpressionResults(out expressionTree, tokenStream, result, parser, parserData);
        }

        public bool ParseExpression(string input, out ExpressionIndex expressionIndex, out ExpressionTree* expressionTree) {
            using TemplateParser parser = TemplateParser.Create();
            ParserData* parserData = CreateParseData(input, parser.panic, out TokenStream tokenStream);
            bool result = parser.ParseCSExpression(ref tokenStream, out expressionIndex);
            return ProcessExpressionResults(out expressionTree, tokenStream, result, parser, parserData);
        }

       
        private ParserData* CreateParseData(string input, bool* panic, out TokenStream tokenStream) {
            
            ParserData* parserData = Native.Memory.AlignedMalloc<ParserData>(1);
            *parserData = default;
            
            parserData->source = new PodList<char>(input.Length);
            parserData->inputTokens = new PodList<Token>(input.Length);

            fixed (char* pInput = input) {
                parserData->source.size = input.Length;
                Native.Memory.MemCpy(parserData->source.GetArrayPointer(), pInput, input.Length);
            }

            // leak on failure, but we don't care. 
            Debug.Assert(Tokenizer.TryTokenize(parserData->source.ToCheckedArray(), &parserData->inputTokens, out _));
            
            parserData->nonTrivialTokens = Token.GatherNonTrivialTokens(parserData->inputTokens.ToCheckedArray());

            FixedCharacterSpan source = parserData->source.ToCheckedArray();

            Token.SetDebugSources(parserData->inputTokens.ToCheckedArray(), source);
            Token.SetDebugSources(parserData->nonTrivialTokens.ToCheckedArray(), source);
            
            parses.Add(new IntPtr(parserData));
            
            tokenStream = new TokenStream(parserData->nonTrivialTokens.ToCheckedArray(), source, panic);
            
            return parserData;
        }

        public void Dispose() {
            for (int i = 0; i < parses.Count; i++) {
                ParserData* pData = (ParserData*)parses[i].ToPointer();
                pData->Dispose();
            }

            parses.Clear();
        }

/*        public TestTemplateTree ParseTestTemplate(string s) {
            Assert.IsTrue(ParseTemplate(s, out TemplateDeclaration result, out TemplateTree* tree), "failed to parse: " + s);
            return new TestTemplateTree() {
                templateDeclaration = result,
                tree = tree,
                source = s
            };
        }

        public TestExpressionTree ParseTestExpression(string s) {
            Assert.IsTrue(ParseExpression(s, out ExpressionIndex result, out ExpressionTree* tree), "failed to parse: " + s);
            return new TestExpressionTree() {
                root = result,
                tree = tree
            };
        }

        public void ParseTestExpression(string s, ExpressionTypeAssert[] asserts, bool print = false) {
            Assert.IsTrue(ParseExpression(s, out ExpressionIndex result, out ExpressionTree* tree), "failed to parse: " + s);
            new TestExpressionTree() {
                root = result,
                tree = tree
            }.AssertTypes(asserts, print);
        }

        public void ParseTestStatements(string s, ExpressionTypeAssert[] asserts, bool print = false) {
            Assert.IsTrue(ParseStatements(s, out ExpressionRange result, out ExpressionTree* tree), "failed to parse: " + s);
            new TestExpressionTree() {
                rootList = result,
                tree = tree
            }.AssertTypes(asserts, print);
        }*/

    }

}
