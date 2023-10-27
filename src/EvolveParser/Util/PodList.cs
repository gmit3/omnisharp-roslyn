using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EvolveUI.Parsing;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Util {

    // this is a native type but since its generic we can use [NativeType]
    internal class PodListDebugView<T> where T : unmanaged {

        public int size;
        public int capacity;
        public T[] data;

        public PodListDebugView(PodList<T> target) {
            this.size = target.size;
            this.capacity = target.capacity;
            this.data = new T[size];
            for (int i = 0; i < size; i++) {
                data[i] = target[i];
            }
        }

    }

    [DebuggerTypeProxy(typeof(PodListDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PodQueue<T> : IDisposable where T : unmanaged {

        private T* array;
        private int head;
        private int tail;
        private int size;
        private int capacity;

        public PodQueue(int capacity) : this() {
            SetCapacity(capacity);
        }

        public int Size => size;

        public void Clear() {
            head = 0;
            tail = 0;
            size = 0;
        }

        public void Enqueue(in T item) {
            if (size >= capacity) {
                SetCapacity(size + 1);
            }

            array[tail] = item;
            tail = (tail + 1) & (capacity - 1);
            ++size;
        }

        public bool TryDequeue(out T retn) {
            if (size == 0) {
                retn = default;
                return false;
            }

            retn = array[head];
            head = (head + 1) & (capacity - 1);
            --size;
            return true;
        }

        private void SetCapacity(int newCapacity) {
            if (newCapacity < 16) newCapacity = 16;
            newCapacity = BitUtil.NextPowerOfTwo(newCapacity);
            T* destinationArray = Native.Memory.AlignedMalloc<T>(newCapacity);
            if (size > 0) {
                if (head < tail) {
                    T* src = &array[head];
                    T* dst = &destinationArray[0];
                    Native.Memory.MemCpy(dst, src, size * sizeof(T));
                }
                else {
                    T* src0 = &array[head];
                    T* dst0 = &destinationArray[0];
                    Native.Memory.MemCpy(dst0, src0, (capacity - head) * sizeof(T));
                    T* src1 = &array[0];
                    T* dst1 = &destinationArray[capacity - head];
                    Native.Memory.MemCpy(dst1, src1, tail * sizeof(T));
                }
            }

            if (array != null) {
                Native.Memory.AlignedFree(array, Native.Memory.AlignOf<T>());
            }

            array = destinationArray;
            head = 0;
            tail = size; //  == capacity ? 0 : size;
            capacity = newCapacity;
        }

        public void Dispose() {
            if (array != null) {
                Native.Memory.AlignedFree(array, Native.Memory.AlignOf<T>());
            }

            this = default;
        }

    }

    [DebuggerTypeProxy(typeof(PodListDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PodStack<T> : IDisposable where T : unmanaged {

        private PodList<T> list;

        public PodStack(int capacity) {
            list = new PodList<T>(capacity);
        }

        public int Count => list.size;

        public void Push(in T item) {
            list.Add(item);
        }

        public T Pop() {
            T retn = list.Get(list.size - 1);
            list.size--;
            return retn;
        }

        public ref T PopRef() {
            ref T retn = ref list.Get(list.size - 1);
            list.size--;
            return ref retn;
        }

        public ref T Peek() {
            return ref list.Get(list.size - 1);
        }

        public void Dispose() {
            list.Dispose();
        }

        public void Clear() {
            list.size = 0;
        }

        public void SetSize(int size) {
            list.size = size;
        }

        public ref T Get(int index) {
            return ref list.Get(index);
        }

        public CheckedArray<T> ToCheckedArray() {
            return list.ToCheckedArray();
        }

    }

    [DebuggerTypeProxy(typeof(PodListDebugView<>))]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PodList<T> : IDisposable where T : unmanaged {

        internal int capacity;
        public int size;
        public T* array;

        public PodList(int capacity) : this() {
            EnsureCapacity(capacity);
        }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetPointer(int idx) {
            TypedUnsafe.CheckRange(idx, size);
            return array + idx;
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

        public CheckedArray<T> ToCheckedArray() {
            return new CheckedArray<T>(array, size);
        }

        public void Clear() {
            size = 0;
        }
        
        public void Add(in T item) {
            if (size >= capacity) {
                EnsureCapacity(capacity == 0 ? 8 : (int)(capacity * 1.5));
            }

            array[size++] = item;
        }

        public T* Reserve() {
            if (size >= capacity) {
                EnsureCapacity(capacity == 0 ? 8 : (int)(capacity * 1.5));
            }

            T* retn = &array[size];
            size++;
            return retn;
        }

        public void AddRange(T* items, int count) {
            if (count == 0) return;
            EnsureAdditionalCapacity(count);
            Native.Memory.MemCpy(array + size, items, count);
            size += count;
        }

        public void SwapRemoveAt(int index) {
            TypedUnsafe.CheckRange(index, size);
            array[index] = array[--size];
        }

        public void EnsureCapacity(int newCapacity) {
            if (capacity >= newCapacity) {
                return;
            }

            newCapacity = Math.Max(8, newCapacity);
            T* newArray = Native.Memory.AlignedMalloc<T>(newCapacity);
            if (array != null) {
                Native.Memory.MemCpy(newArray, array, size);
                Native.Memory.AlignedFree(array, Native.Memory.AlignOf<T>());
            }

            array = newArray;
            capacity = newCapacity;
        }

        public void EnsureAdditionalCapacity(int additional) {
            EnsureCapacity(size + additional);
        }

        public void Dispose() {
            if (array != null) {
                Native.Memory.AlignedFree(array, Native.Memory.AlignOf<T>());
            }

            this = default;
        }

        public T* GetArrayPointer() {
            return array;
        }

        public T[] ToArray() {
            return ToCheckedArray().ToArray();
        }

    }

}