using System.Diagnostics;

namespace EvolveUI.Parsing {
    public struct ExpressionIndex {

        public int id;

        // need to use > 0 because when we compress invalid / unset nodes
        // their id will already be 0 and will now shift into negative values

        public bool IsValid => id > 0;

        [DebuggerStepThrough]
        public ExpressionIndex(int index) {
            this.id = index;
        }

        public ExpressionRangeIndex<T> As<T>() where T : unmanaged, IExpressionNode{
            return new ExpressionRangeIndex<T>(id);
        }

    }

    public struct ExpressionIndex<T> where T : unmanaged, IExpressionNode {

        public int id;

        // need to use > 0 because when we compress invalid / unset nodes
        // their id will already be 0 and will now shift into negative values

        public bool IsValid => id > 0;

        [DebuggerStepThrough]
        public ExpressionIndex(int index) {
            this.id = index;
        }

        [DebuggerStepThrough]
        public ExpressionIndex(ExpressionIndex expressionIndex) {
            this.id = expressionIndex.id;
        }

        [DebuggerStepThrough]
        public static implicit operator ExpressionIndex(ExpressionIndex<T> self) {
            return new ExpressionIndex(self.id);
        }

    }

    public struct ExpressionRange<T> where T : unmanaged, IExpressionNode {

        public ushort start;
        public ushort length;

        public int end => start + length;

        public ExpressionRange(ushort start, ushort length) {
            this.start = start;
            this.length = length;
        }
        
        public static implicit operator ExpressionRange(ExpressionRange<T> range) {
            return new ExpressionRange() {
                start = range.start,
                length = range.length
            };
        }

        public ExpressionRangeIndex<T> this[int offset] {
            get => new ExpressionRangeIndex<T>(start + offset);
        }

    }

    public struct ExpressionRange {

        public ushort start;
        public ushort length;

        public int end => start + length;
        
        public ExpressionRange(ushort start, ushort length) {
            this.start = start;
            this.length = length;
        }
        
        public ExpressionRangeIndex this[int offset] {
            get => new ExpressionRangeIndex(start + offset);
        }

    }

    public struct ExpressionRangeIndex {

        public int index;

        public ExpressionRangeIndex(int index) {
            this.index = index;
        }

    }

    public struct ExpressionRangeIndex<T> where T : unmanaged, IExpressionNode {

        public int index;

        public ExpressionRangeIndex(int index) {
            this.index = index;
        }

    }

}
