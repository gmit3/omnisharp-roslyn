using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EvolveUI.Util.Unsafe;

namespace EvolveUI {

    [DebuggerDisplay("size = {" + nameof(size) + "}")]
    [DebuggerTypeProxy(typeof(CheckedArrayDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CheckedArray<T> where T : unmanaged {

        private readonly T* array;
        public readonly int size;

        [DebuggerStepThrough]
        public CheckedArray(T* array, int size) {
            this.array = array;
            this.size = size;
        }

        public Span<T> ToSpan() {
            return new Span<T>(array, size);
        }

        [DebuggerStepThrough]
        public CheckedArray(byte* array, int size) {
            this.array = (T*)array;
            this.size = size;
        }

        [DebuggerStepThrough]
        public CheckedArray(T* buffer, RangeInt range) {
            size = range.length;
            array = buffer + range.start;
        }

        // [DebuggerStepThrough]
        // public CheckedArray(DataList<T> list) {
        //     size = list.size;
        //     array = list.GetArrayPointer();
        // }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int idx) {
            TypedUnsafe.CheckRange(idx, size);
            return ref array[idx];
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(uint idx) {
            TypedUnsafe.CheckRange(idx, size);
            return ref array[idx];
        }

        [DebuggerStepThrough]
        public void Set(int idx, in T value) {
            TypedUnsafe.CheckRange(idx, size);
            array[idx] = value;
        }

        [DebuggerStepThrough]
        public void Set(uint idx, in T value) {
            TypedUnsafe.CheckRange(idx, size);
            array[idx] = value;
        }

        public T this[int idx] {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                TypedUnsafe.CheckRange(idx, size);
                return array[idx];
            }
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                TypedUnsafe.CheckRange(idx, size);
                array[idx] = value;
            }
        }

        public T this[uint idx] {
            [DebuggerStepThrough]
            get {
                TypedUnsafe.CheckRange(idx, size);
                return array[idx];
            }
            [DebuggerStepThrough]
            set {
                TypedUnsafe.CheckRange(idx, size);
                array[idx] = value;
            }
        }

        public int BinarySearch(in T value, IComparer<T> comp) {
            int offset = 0;

            for (int l = size; l != 0; l >>= 1) {
                int idx = offset + (l >> 1);
                int r = comp.Compare(value, array[idx]);
                if (r == 0) {
                    return idx;
                }

                if (r > 0) {
                    offset = idx + 1;
                    --l;
                }
            }

            return ~offset;
        }

        [DebuggerStepThrough]
        public T[] ToArray() {
            T[] dst = new T[size];
            for (int i = 0; i < size; i++) {
                dst[i] = array[i];
            }

            return dst;
        }

        [DebuggerStepThrough]
        public T* GetArrayPointer() {
            return array;
        }

        [DebuggerStepThrough]
        public CheckedArray<T> Slice(int start, int count) {
            if (count == 0) return new CheckedArray<T>(array, 0);
            TypedUnsafe.CheckRange(start, size);
            TypedUnsafe.CheckRange(start + count - 1, size);
            return new CheckedArray<T>(array + start, count);
        }

        [DebuggerStepThrough]
        public CheckedArray<T> Slice(RangeInt range) {
            return Slice(range.start, range.length);
        }
        [DebuggerStepThrough]
        public CheckedArray<T> Slice(int range) {
            return Slice(0, range);
        }
        
        [DebuggerStepThrough]
        public T* GetPointer(int offset) {
            TypedUnsafe.CheckRange(offset, size);
            return array + offset;
        }

        [DebuggerStepThrough]
        public U* GetTypedPointer<U>(int offset) where U : unmanaged {
            // convert range checking to bytes since there may be a mismatch between sizeof(T) and sizeof(U)
            TypedUnsafe.CheckRange(offset * sizeof(T) + sizeof(U), size * sizeof(T));
            return (U*)(array + offset);
        }

        public void Clear() {
            Native.Memory.MemClear(array, size);
        }

        // public void Sort<TSorter>(TSorter sorter) where TSorter : IComparer<T> {
        //     NativeSortExtension.Sort(array, size, sorter);
        // }

        public void CopyFrom(CheckedArray<T> other) {
            Native.Memory.MemCpy(array, other.array, other.size);
        }

    }

    internal sealed class CheckedArrayDebugView<T> where T : unmanaged {

        private CheckedArray<T> m_Array;

        public CheckedArrayDebugView(CheckedArray<T> array) => this.m_Array = array;

        public T[] Items => this.m_Array.ToArray();

    }

}