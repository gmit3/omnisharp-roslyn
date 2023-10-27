using System.Diagnostics;
using System.Runtime.InteropServices;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Util {

    // not disposable, doesn't own the memory
    [DebuggerDisplay("{ToString()}")]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FixedCharacterSpan : IEquatable<FixedCharacterSpan> {

        public readonly int size;
        public readonly char* ptr;

        public FixedCharacterSpan(char* ptr, int size) {
            this.ptr = ptr;
            this.size = size;
        }

        public FixedCharacterSpan(CheckedArray<char> checkedArray) {
            this.ptr = checkedArray.GetArrayPointer();
            this.size = checkedArray.size;
        }

        public char this[int index] {
            get {
                TypedUnsafe.CheckRange(index, size);
                return ptr[index];
            }
        }

        public static bool operator ==(FixedCharacterSpan a, string b) {
            if (b == null) {
                return a.size == 0;
            }

            fixed (char* cbuffer = b) {
                return a.Equals(new FixedCharacterSpan(cbuffer, b.Length));
            }
        }

        public static bool operator !=(FixedCharacterSpan a, string b) {
            return !a.Equals(b);
        }

        public static bool operator ==(FixedCharacterSpan a, FixedCharacterSpan b) {
            return a.Equals(b);
        }

        public static bool operator !=(FixedCharacterSpan a, FixedCharacterSpan b) {
            return !a.Equals(b);
        }

        public bool Equals(string other) {

            if (other == null) return size == 0;

            if (size != other.Length) return false;

            fixed (char* cbuffer = other) {
                return Native.Memory.MemCmp(ptr, cbuffer, size);
            }
        }
    
        public bool Equals(FixedCharacterSpan other) {
            return size == other.size && Native.Memory.MemCmp(ptr, other.ptr, size);
        }

        public override bool Equals(object obj) {
            return obj is FixedCharacterSpan other && Equals(other);
        }

        public override int GetHashCode() {
            return HashUtil.StringHash(ptr, size);
        }

        public CheckedArray<char> ToCheckedArray() {
            return new CheckedArray<char>(ptr, size);
        }

        public override string ToString() {
            if (ptr == null || size == 0) return string.Empty;
            return new string(ptr, 0, size);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan Slice(int start, int count) {
            if (count == 0) return new FixedCharacterSpan(ptr, 0);
            TypedUnsafe.CheckRange(start, size);
            TypedUnsafe.CheckRange(start + count - 1, size);
            return new FixedCharacterSpan(ptr + start, count);
        }

        [DebuggerStepThrough]
        public FixedCharacterSpan Slice(RangeInt range) {
            return Slice(range.start, range.length);
        }

        public char* GetPointer(int start) {
            TypedUnsafe.CheckRange(start, size);
            return ptr + start;
        }

        public static implicit operator FixedCharacterSpan(CheckedArray<char> checkedArray) {
            return new FixedCharacterSpan(checkedArray);
        }

    }

}