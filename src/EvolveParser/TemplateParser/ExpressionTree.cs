using System;
using System.Diagnostics;
using EvolveUI.Util;

namespace EvolveUI.Parsing {

    /// <summary>
    /// Not responsible for memory!
    /// </summary>
    public partial struct ExpressionTree {

        public FixedCharacterSpan source;
        public CheckedArray<Token> nonTrivialTokens;
        public CheckedArray<UntypedExpressionNode> untypedNodes;
        public CheckedArray<ExpressionIndex> ranges;

        public void Visit(ExpressionIndex expression, Action<UntypedExpressionNode> nodeFn) {
            VisitImpl(nodeFn, expression);
        }

        public void Visit(ExpressionRange range, Action<UntypedExpressionNode> nodeFn) {
            VisitRange(nodeFn, range.start, range.length);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan GetTokenSource(NonTrivialTokenRange tokenRange) {
            int start = tokenRange.start.index;
            int end = tokenRange.end.index;
            int startLocation = nonTrivialTokens[start].charIndex;
            int endLocation = nonTrivialTokens[end - 1].charIndex + nonTrivialTokens[end - 1].length;
            return source.Slice(startLocation, endLocation - startLocation);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan GetTokenSource(ExpressionIndex expressionIndex) {
            NonTrivialTokenRange tokenRange = GetTokenRange(expressionIndex);
            int start = tokenRange.start.index;
            int end = tokenRange.end.index;
            int startLocation = nonTrivialTokens[start].charIndex;
            int endLocation = nonTrivialTokens[end - 1].charIndex + nonTrivialTokens[end - 1].length;
            return source.Slice(startLocation, endLocation - startLocation);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan GetTokenSource(NonTrivialTokenLocation index) {
            if (index.index <= 0) return default;
            Token token = nonTrivialTokens[index.index];
            return source.Slice(token.charIndex, token.length);
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(NonTrivialTokenRange range) {
            return GetTokenSource(range).ToString();
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(NonTrivialTokenLocation index) {
            Token token = nonTrivialTokens[index.index];
            return source.Slice(token.charIndex, token.length).ToString();
        }

        [DebuggerStepThrough]
        public string GetTokenSourceString(ExpressionIndex index) {
            UntypedExpressionNode node = untypedNodes[index.id];
            NonTrivialTokenRange range = node.meta.tokenRange;
            return GetTokenSource(range).ToString();
        }

        public NonTrivialTokenRange GetTokenRange(ExpressionRange range) {
            if (range.length == 0) return default;
            ExpressionIndex startId = ranges[range.start];
            ExpressionIndex endId = ranges[range.start + range.length - 1];
            TokenIndex rangeStart = Get(startId).meta.tokenRange.start;
            TokenIndex rangeEnd = Get(endId).meta.tokenRange.end;
            return new NonTrivialTokenRange(rangeStart.index, rangeEnd.index);
        }

        [DebuggerStepThrough]
        public UntypedExpressionNode Get(ExpressionRangeIndex index) {
            return untypedNodes[ranges[index.index].id];
        }

        [DebuggerStepThrough]
        public T Get<T>(ExpressionRangeIndex<T> index) where T : unmanaged, IExpressionNode {
            return untypedNodes[ranges[index.index].id].As<T>();
        }

        [DebuggerStepThrough]
        public ExpressionIndex<T> GetIndex<T>(ExpressionRangeIndex<T> index) where T : unmanaged, IExpressionNode {
            return new ExpressionIndex<T>(ranges[index.index].id);
        }
        
        [DebuggerStepThrough]
        public ExpressionIndex GetIndex(ExpressionRangeIndex index)  {
            return new ExpressionIndex(ranges[index.index].id);
        }
        
        [DebuggerStepThrough]
        public UntypedExpressionNode Get(ExpressionIndex index) {
            return untypedNodes[index.id];
        }

        [DebuggerStepThrough]
        public T Get<T>(ExpressionIndex<T> index) where T : unmanaged, IExpressionNode {
            return untypedNodes[index.id].As<T>();
        }

        [DebuggerStepThrough]
        public NonTrivialTokenRange GetTokenRange(ExpressionIndex index) {
            return untypedNodes[index.id].meta.tokenRange;
        }

        partial void VisitImpl(Action<UntypedExpressionNode> action, ExpressionIndex index);

        private void VisitRange(Action<UntypedExpressionNode> action, int start, int count) {
            for (int i = start; i < start + count; i++) {
                VisitImpl(action, ranges[i]);
            }
        }

        public void Visit<T>(ref T visitor, ExpressionIndex nodeIndex) where T : IExpressionVisitor {
            InterfaceVisitImpl(ref visitor, nodeIndex);
        }

        public void Visit<T>(ref T visitor, ExpressionRange nodeRange) where T : IExpressionVisitor {
            int end = nodeRange.start + nodeRange.length;
            for (int i = nodeRange.start; i < end; i++) {
                InterfaceVisitImpl(ref visitor, ranges[i]);
            }
        }

        partial void InterfaceVisitImpl<T>(ref T visitor, ExpressionIndex index) where T : IExpressionVisitor;

    }

    public enum VisitorAction {

        Visit,
        DoNotVisit

    }

    public partial interface IExpressionVisitor { }

    internal unsafe ref partial struct ExpressionPrintingVisitor {

        private readonly ExpressionTree* tree;

        private ExpressionPrintingVisitor(ExpressionTree* tree) {
            this.tree = tree;
        }

        public static void Print(IndentedStringBuilder builder, ExpressionIndex expressionIndex, ExpressionTree* expressionTree) {
            new ExpressionPrintingVisitor(expressionTree).Visit(builder, expressionIndex);
        }

        public static string Print(ExpressionRange expressionRange, ExpressionTree* expressionTree) {
            IndentedStringBuilder builder = new IndentedStringBuilder(1024);
            new ExpressionPrintingVisitor(expressionTree).VisitRange(builder, expressionRange.start, expressionRange.length);
            return builder.ToString();
        }

        public static string Print(ExpressionIndex expressionIndex, ExpressionTree* expressionTree) {
            IndentedStringBuilder builder = new IndentedStringBuilder(1024);
            new ExpressionPrintingVisitor(expressionTree).Visit(builder, expressionIndex);
            return builder.ToString();
        }

        partial void VisitImpl(IndentedStringBuilder builder, ExpressionIndex index);

        public void VisitRange(IndentedStringBuilder builder, int start, int count) {

            for (int i = start; i < start + count; i++) {
                VisitImpl(builder, tree->ranges[i]);
            }

        }

        public void Visit(IndentedStringBuilder builder, ExpressionIndex expressionIndex) {
            VisitImpl(builder, expressionIndex);
        }

    }

}
