using System;
using System.Diagnostics;
using EvolveUI.Util;

namespace EvolveUI.Parsing {
    public partial interface ITemplateVisitor { }

    /// <summary>
    /// Not responsible for memory!
    /// </summary>
    public unsafe partial struct TemplateTree {

        public FixedCharacterSpan source;
        public CheckedArray<Token> nonTrivialTokens;
        public CheckedArray<UntypedTemplateNode> untypedNodes;
        public CheckedArray<NodeIndex> ranges;

        public ExpressionTree* expressionTree;

        [DebuggerStepThrough]
        public UntypedTemplateNode Get(NodeRangeIndex index) {
            return untypedNodes[ranges[index.index].id];
        }

        // [DebuggerStepThrough]
        public T Get<T>(NodeRangeIndex<T> index) where T : unmanaged, ITemplateNode {
            return untypedNodes[ranges[index.index].id].As<T>();
        }

        public NodeIndex GetIndex(NodeRangeIndex index) {
            return new NodeIndex(ranges[index.index].id);
        }

        [DebuggerStepThrough]
        public UntypedTemplateNode Get(NodeIndex index) {
            return untypedNodes[index.id];
        }

        [DebuggerStepThrough]
        public T Get<T>(NodeIndex<T> index) where T : unmanaged, ITemplateNode {
            return untypedNodes[index.id].As<T>();
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan GetTokenSource(NonTrivialTokenRange tokenRange) {
            
            if (!tokenRange.IsValid) {
                return default;
            }
            
            int start = tokenRange.start.index;
            int end = tokenRange.end.index;
            int startLocation = nonTrivialTokens[start].charIndex;
            int endLocation = nonTrivialTokens[end - 1].charIndex + nonTrivialTokens[end - 1].length;
            return source.Slice(startLocation, endLocation - startLocation);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan GetTokenSource(NonTrivialTokenLocation nonTrivial) {
            int startLocation = nonTrivialTokens[nonTrivial.index].charIndex;
            return source.Slice(startLocation, nonTrivialTokens[nonTrivial.index].length);
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(int index) {
            UntypedTemplateNode node = untypedNodes[index];
            NonTrivialTokenRange range = node.meta.tokenRange;
            FixedCharacterSpan tokenSource = GetTokenSource(range);
            return new string(tokenSource.ptr, 0, source.size);
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(NonTrivialTokenRange range) {
            FixedCharacterSpan tokenSource = GetTokenSource(range);
            return new string(tokenSource.ptr, 0, tokenSource.size);
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(NonTrivialTokenLocation index) {
            return GetTokenSource(new NonTrivialTokenRange(index.index, index.index + 1)).ToString();
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(UntypedExpressionNode node) {
            return GetTokenSource(node.meta.tokenRange).ToString();
        }

        [DebuggerStepThrough]
        public NonTrivialTokenRange GetTokenRange(NonTrivialTokenLocation location) {
            int startLocation = nonTrivialTokens[location.index].tokenIndex;
            return new NonTrivialTokenRange(startLocation, startLocation + 1);
        }

        [DebuggerStepThrough]
        public NonTrivialTokenRange GetTokenRange(ExpressionRange range) {
            int startExpression = expressionTree->ranges[range.start].id;
            int endExpression = expressionTree->ranges[range.end - 1].id;
            TokenIndex startToken = expressionTree->untypedNodes[startExpression].meta.tokenRange.start;
            TokenIndex endToken = expressionTree->untypedNodes[endExpression].meta.tokenRange.end;
            return new NonTrivialTokenRange(startToken.index, endToken.index);
        }

        public void Visit(NodeIndex nodeIndex, Action<UntypedTemplateNode> nodeFn) {
            VisitImpl(nodeFn, nodeIndex);
        }

        public void VisitRange(Action<UntypedTemplateNode> action, int start, int length) {
            for (int i = start; i < start + length; i++) {
                VisitImpl(action, ranges[i]);
            }
        }

        public void Visit<T>(ref T visitor, NodeIndex nodeIndex) where T : ITemplateVisitor {
            InterfaceVisitImpl(ref visitor, nodeIndex);
        }

        public void Visit<T>(ref T visitor, NodeRange nodeRange) where T : ITemplateVisitor {
            int end = nodeRange.start + nodeRange.length;
            for (int i = nodeRange.start; i < end; i++) {
                InterfaceVisitImpl(ref visitor, ranges[i]);
            }
        }

        partial void VisitImpl(Action<UntypedTemplateNode> action, NodeIndex index);

        partial void InterfaceVisitImpl<T>(ref T visitor, NodeIndex index) where T : ITemplateVisitor;

    }

}
