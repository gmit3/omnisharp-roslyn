using System.Diagnostics;

namespace EvolveUI.Parsing {

    [DebuggerDisplay("{GetDebuggerDisplay()}")]
    public struct NonTrivialTokenRange {

        public TokenIndex start;
        public TokenIndex end;

        public NonTrivialTokenRange(int start, int end) {
            this.start = new TokenIndex(start);
            this.end = new TokenIndex(end);
        }

        public NonTrivialTokenRange(in Token token) {
            this.start = new TokenIndex(token.tokenIndex);
            this.end = new TokenIndex(token.tokenIndex);
        }

        public NonTrivialTokenRange(NonTrivialTokenLocation nodeIdentifierLocation) {
            this.start = nodeIdentifierLocation.index;
            this.end = nodeIdentifierLocation.index + 1;
        }

        public static implicit operator NonTrivialTokenRange(int index) {
            return new NonTrivialTokenRange(index, index + 1);
        }

        public bool IsValid => start.index != 0 && end.index != 0;

        public string GetDebuggerDisplay() {
            return $"start = {start.index}, end = {end.index}";
        }

        public static NonTrivialTokenRange Combine(NonTrivialTokenRange lhsTokenRange, NonTrivialTokenRange rhsTokenRange) {
            throw new System.NotImplementedException();
        }

    }

    [DebuggerDisplay("{index}")]
    public struct NonTrivialTokenLocation {

        public readonly int index;

        public NonTrivialTokenLocation(int index) {
            this.index = index;
        }

        public bool IsValid => index > 0;

    }

    [DebuggerDisplay("{index}")]
    public struct TokenIndex {

        public readonly ushort index;

        public TokenIndex(int index) {
            this.index = (ushort)index;
        }

        public TokenIndex(in Token token) {
            this.index = (ushort)token.tokenIndex;
        }

        public static implicit operator TokenIndex(int index) {
            return new TokenIndex(index);
        }

    }

}
