using System;
using System.Diagnostics;
using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {
    public unsafe ref partial struct TemplateParser {

        public struct RuleResult {

            public int endStreamLocation;
            public bool successful;
            public ExpressionIndex result;

            [DebuggerStepThrough]
            public RuleResult(int location, ExpressionIndex result) {
                this.successful = true;
                this.result = result;
                this.endStreamLocation = location;
            }

        }

        public struct RuleResult<T> where T : unmanaged, IExpressionNode {

            public int endStreamLocation;
            public ExpressionIndex<T> result;

            [DebuggerStepThrough]
            public RuleResult(int location, ExpressionIndex<T> result) {
                this.result = result;
                this.endStreamLocation = location;
            }

        }

        internal struct BufferScope {

            public const int k_MaxRuleCount = 20; // max scope counts in use atm + a bit of buffer 

            public int currentRule;
            public int baseRangeOffset;
            public int baseExpressionOffset;

            public fixed ushort expressionOffsets[k_MaxRuleCount];
            public fixed ushort rangeOffsets[k_MaxRuleCount];
            public fixed short ruleLengths[k_MaxRuleCount];

            public BufferScope(int expressionsSize, int rangeBufferSize) : this() {
                this.currentRule = 0;
                this.baseRangeOffset = rangeBufferSize;
                this.baseExpressionOffset = expressionsSize;
                expressionOffsets[0] = (ushort)expressionsSize;
                rangeOffsets[0] = (ushort)rangeBufferSize;

            }

        }

        private partial struct ExpressionBuffer : IDisposable {

            private PodStack<BufferScope> scopes;
            internal PodList<UntypedExpressionNode> expressions;
            internal PodList<ExpressionIndex> rangeBuffer;

            public ExpressionBuffer(int scopeCount, int expressionCapacity) {
                this.scopes = new PodStack<BufferScope>(scopeCount);
                this.expressions = new PodList<UntypedExpressionNode>(expressionCapacity);
                this.rangeBuffer = new PodList<ExpressionIndex>(expressionCapacity);
                this.expressions.size = 1;
                this.rangeBuffer.size = 1;
            }

            [DebuggerStepThrough]
            public RuleResult<T> AddRuleResult<T>(int start, int end, in T node) where T : unmanaged, IExpressionNode {
                return new RuleResult<T>(end, Add(start, end, node));
            }

            [DebuggerStepThrough]
            public ExpressionIndex<T> Add<T>(int start, int end, in T node) where T : unmanaged, IExpressionNode {
                return Add(node, new NonTrivialTokenRange(start, end));
            }

            [DebuggerStepThrough]
            public ExpressionIndex<T> Add<T>(T node, NonTrivialTokenRange tokenRange) where T : unmanaged, IExpressionNode {
                if (node.NodeType == ExpressionNodeType.Invalid) {
                    LogUtil.Log("Invalid added!");
                }

                UntypedExpressionNode untypedNode = *(UntypedExpressionNode*)&node;
                untypedNode.meta = new ExpressionNodeHeader() {
                    type = node.NodeType,
                    nodeIndexShort = default,
                    tokenRange = tokenRange
                };
                expressions.Add(untypedNode);
                return new ExpressionIndex<T>(expressions.size - 1);
            }


            partial void CompactNodes(int expressionShift, int rangeShift, int nodeStart, int nodeEnd);

            [DebuggerStepThrough]
            public void PushRuleScope() {
                scopes.Push(new BufferScope(expressions.size, rangeBuffer.size));
            }

            public bool PopRuleScope(ref TokenStream tokenStream, out ExpressionIndex winningIndex) {
                ref BufferScope last = ref scopes.PopRef();

                int best = -1;
                int winningRuleIndex = -1;

                for (int i = 0; i < last.currentRule; i++) {

                    if (last.ruleLengths[i] == 0) {
                        continue;
                    }

                    if (last.ruleLengths[i] > best) {
                        best = last.ruleLengths[i];
                        winningRuleIndex = i;
                    }

                }

                int scopeExpressionStart = last.baseExpressionOffset;
                int scopeRangeStart = last.baseRangeOffset;

                int START_VAL = expressions.size;

                if (winningRuleIndex >= 0) {
                    // commit the rules to the output buffer
                    int expressionStartIndex = last.expressionOffsets[winningRuleIndex]; // expression buffer size when rule ws created 
                    int expressionEndIndex = winningRuleIndex == last.currentRule - 1 // expression buffer size when next rule was created or full buffer size 
                        ? expressions.size
                        : last.expressionOffsets[winningRuleIndex + 1];
                    int expressionCount = expressionEndIndex - expressionStartIndex;

                    int rangeStartIndex = last.rangeOffsets[winningRuleIndex];
                    int rangeEndIndex = winningRuleIndex == last.currentRule - 1
                        ? rangeBuffer.size
                        : last.rangeOffsets[winningRuleIndex + 1];

                    int rangeSize = rangeEndIndex - rangeStartIndex;
                    int expressionShift = expressionStartIndex - last.baseExpressionOffset;
                    int rangeShift = rangeStartIndex - last.baseRangeOffset;

                    if (winningRuleIndex != 0) {
                        // no copy needed if the 0 rule won since the expressions are already seated properly
                        if (expressionCount > 0) {
                            Native.Memory.MemCpy(expressions.GetPointer(last.baseExpressionOffset), expressions.GetPointer(expressionStartIndex), expressionCount);
                        }

                        if (rangeSize > 0) {
                            Native.Memory.MemCpy(rangeBuffer.GetPointer(last.baseRangeOffset), rangeBuffer.GetPointer(rangeStartIndex), rangeSize);
                        }

                        // shift all the node references up by the the rule positions
                        CompactNodes(
                            expressionShift,
                            rangeShift,
                            last.baseExpressionOffset,
                            last.baseExpressionOffset + expressionCount
                        );

                        for (int i = scopeRangeStart; i < scopeRangeStart + rangeSize; i++) {
                            rangeBuffer.Get(i).id -= expressionShift;
                        }

                    }

                    expressions.size = scopeExpressionStart + expressionCount;
                    rangeBuffer.size = scopeRangeStart + rangeSize;
                    winningIndex = new ExpressionIndex(expressions.size - 1); //last.expressionIndices[winningRuleIndex] - expressionShift);

                    tokenStream.location = best;
                    int END_VAL = expressions.size;
                    if (END_VAL > START_VAL) {
                        LogUtil.Log("ADDED DURING COMPRESS");
                    }

                    return true;
                }

                expressions.size = scopeExpressionStart;
                rangeBuffer.size = scopeRangeStart;
                winningIndex = default;
                int END_VAL2 = rangeBuffer.size;
                if (END_VAL2 > START_VAL) {
                    LogUtil.Log("ADDED DURING COMPRESS NO OP");
                }

                return false;
            }

            public bool PopRuleScope(ref TokenStream tokenStream) {
                return PopRuleScope(ref tokenStream, out _);
            }

            [DebuggerStepThrough]
            public void MakeRule<T>(in RuleResult<T> result) where T : unmanaged, IExpressionNode {
                MakeRule(new RuleResult(result.endStreamLocation, new ExpressionIndex(result.result.id)));
            }

            [DebuggerStepThrough]
            public void MakeRule(in RuleResult result) {
                ref BufferScope scope = ref scopes.Peek();

                // scope.expressionIndices[scope.currentRule] = result.result.id;
       
                scope.ruleLengths[scope.currentRule] = result.successful
                    ? (short)result.endStreamLocation
                    : (short)0;
                scope.currentRule++;
                AssertMaxRuleCount(scope.currentRule);
                scope.expressionOffsets[scope.currentRule] = (ushort)(expressions.size);
                scope.rangeOffsets[scope.currentRule] = (ushort)rangeBuffer.size;
            }

            [Conditional("EVOLVE_UI_DEV")]
            private void AssertMaxRuleCount(int ruleCount) {
                if (ruleCount >= BufferScope.k_MaxRuleCount) {
                    throw new Exception("Max rule count exceeded");
                }
            }

            public void Dispose() {
                scopes.Dispose();
                expressions.Dispose();
                rangeBuffer.Dispose();
            }

            public void Clear() {
                this.expressions.size = 1;
                this.rangeBuffer.size = 1;
                scopes.Clear();
            }

            public ExpressionRange<T> AddExpressionList<T>(ScopedList<ExpressionIndex<T>> buffer) where T : unmanaged, IExpressionNode {
                ExpressionRange<T> range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = (ushort)buffer.size;
                rangeBuffer.AddRange((ExpressionIndex*)buffer.array, buffer.size);
                return range;
            }

            public ExpressionRange AddExpressionList(ExpressionIndex index) {
                ExpressionRange range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = 1;
                rangeBuffer.Add(index);
                return range;
            }

            public ExpressionRange<T> AddExpressionList<T>(ExpressionIndex<T> index) where T : unmanaged, IExpressionNode {
                ExpressionRange<T> range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = 1;
                rangeBuffer.Add(index);
                return range;
            }

            public ExpressionRange AddExpressionList(ScopedList<ExpressionIndex> buffer) {
                ExpressionRange range = default;
                range.start = (ushort)rangeBuffer.size;
                range.length = (ushort)buffer.size;
                rangeBuffer.AddRange(buffer.array, buffer.size);
                return range;
            }

        }

    }

}
