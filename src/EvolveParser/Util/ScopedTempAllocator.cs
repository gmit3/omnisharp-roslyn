using System;
using System.Runtime.InteropServices;
using EvolveUI.Native;

namespace EvolveUI.Util.Unsafe {

    // can't be ref struct due to ptr requirement 
    internal unsafe struct ScopedTempAllocator : IDisposable {

        public byte* start { get; private set; }
        public byte* current { get; private set; }
        public byte* end { get; private set; }
        public int currentScopeId { get; private set; }

        private PodList<OverflowAlloc> overflows;

        public ScopedTempAllocator(MemoryPool pool) {
            this.start = pool.allocation;
            this.current = start;
            this.end = current + pool.capacityInBytes;
            this.currentScopeId = 0;
            this.overflows = default;
        }

        public ScopedTempAllocator(byte* allocation, long capacityInBytes) {
            this.start = allocation;
            this.current = start;
            this.end = current + capacityInBytes;
            this.currentScopeId = 0;
            this.overflows = default;
        }

        public void Dispose() {
            for (int i = 0; i < overflows.size; i++) {
                Memory.AlignedFree(overflows[i].ptr, overflows[i].alignment);
            }

            overflows.Dispose();
            this = default;
        }

        public void Clear() {
            current = start;
            currentScopeId = 0;
            for (int i = 0; i < overflows.size; i++) {
                Memory.AlignedFree(overflows[i].ptr, overflows[i].alignment);
            }

            overflows.Dispose();
        }

        public void AssertEmpty() {
            if (current != start) {
                throw new Exception("ScopedTempAllocator should be empty");
            }
        }

        public ScopedList<T> CreateListScope<T>(int itemCapacity) where T : unmanaged {
            currentScopeId++;
            byte* scopeOffset = current;
            T* ptr = Allocate<T>(currentScopeId, itemCapacity);
            return new ScopedList<T>((ScopedTempAllocator*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this), ptr, itemCapacity, scopeOffset, currentScopeId);
        }
        
        public AllocatorScope PushScope() {
            currentScopeId++;
            return new AllocatorScope((ScopedTempAllocator*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this), currentScopeId);
        }

        public LooseAllocatorScope PushLooseScope() {
            currentScopeId++;
            return new LooseAllocatorScope(currentScopeId, current);
        }

        public void PopScope<T>(LooseScopedList<T> looseAllocatorScope) where T : unmanaged {
            PopScope(looseAllocatorScope.scope);
        }
        
        public void PopScope(LooseAllocatorScope looseAllocatorScope) {
            if (looseAllocatorScope.scopeId != currentScopeId) {
                throw new Exception("Invalid Allocator scope pop");
            }

            current = looseAllocatorScope.offset;
            currentScopeId--;
        }

        public void PopScope(int scopeId, byte* offset) {
            if (scopeId != currentScopeId) {
                throw new Exception("Invalid Allocator scope pop");
            }

            current = offset;
            currentScopeId--;
        }

        public void PopScope(AllocatorScope scope) {

            if (scope.scopeId != currentScopeId) {
                throw new Exception("Invalid Allocator scope pop");
            }

            current = scope.offset;
            currentScopeId--;

        }

        public ScopedList<T> CreateList<T>(in AllocatorScope scope, int itemCapacity) where T : unmanaged {
            if (scope.scopeId != currentScopeId) {
                throw new Exception("Invalid list allocation, scope is not active");
            }

            T* ptr = Allocate<T>(scope.scopeId, itemCapacity);

            return new ScopedList<T>((ScopedTempAllocator*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this), scope, ptr, itemCapacity);
        }

        public T* Reallocate<T>(int scopeId, T* array, int currentCapacityInItems, int newCapacityInItems) where T : unmanaged {
            if (currentScopeId != scopeId) {
                throw new Exception("Invalid scope allocation");
            }

            // this was an overflow pointer if not in our pool range
            if (array < start || array >= end) {
                // don't bother freeing it, will be handled on the last scope pop
                return OverflowAllocate<T>(newCapacityInItems);
            }

            long byteCount = sizeof(T) * currentCapacityInItems;
            // todo -- this isn't handling alignment at all so if the input ptr exactly == current then we falsly do a new allocate instead of ptr bump
            if (array == current - byteCount) {

                // overflow allocation fall back to Unity
                if (current + byteCount >= end) {
                    return OverflowAllocate<T>(newCapacityInItems);
                }

                current += byteCount;
                return array;
            }

            return Allocate<T>(scopeId, newCapacityInItems);

        }

        private T* OverflowAllocate<T>(int itemCount) where T : unmanaged {
            T* alloc = Native.Memory.AlignedMalloc<T>(itemCount);
            if (overflows.GetArrayPointer() == null) {
                overflows = new PodList<OverflowAlloc>(8);
            }
            overflows.Add(new OverflowAlloc() {
                capacity = itemCount * sizeof(T),
                ptr = alloc,
                alignment = Native.Memory.AlignOf<T>()
            });
            return alloc;
        }

        public T* Allocate<T>(int scopeId, int itemCount) where T : unmanaged {

            // some other assert?
            if (currentScopeId != scopeId) {
                return null;
            }

            // figure out aligned size needed for pointer
            int rawByteSize = sizeof(T) * itemCount;
            int alignmentPowerOfTwo = Native.Memory.AlignOf<T>();
            int alignedSize = (rawByteSize + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);

            // overflow handling falls back to unity
            if (current + alignedSize >= end) {
                return OverflowAllocate<T>(itemCount);
            }

            // bump pointer and return it properly aligned to T size 
            byte* newCurrent = (byte*)current + alignedSize;
            byte* retn = current + (alignedSize - rawByteSize);
            current = newCurrent;
            return (T*)retn;
        }

        private struct OverflowAlloc {

            public void* ptr;
            public long capacity;
            public long alignment;

        }

    }

    internal unsafe struct LooseAllocatorScope {

        public readonly int scopeId;
        public readonly byte* offset;

        internal LooseAllocatorScope(int scopeId, byte* offset) {
            this.scopeId = scopeId;
            this.offset = offset;
        }

    }

    internal unsafe ref struct AllocatorScope {

        private readonly ScopedTempAllocator* tempAllocator;
        public readonly int scopeId;
        public readonly byte* offset;

        internal AllocatorScope(ScopedTempAllocator* tempAllocator, int scopeId) {
            this.tempAllocator = tempAllocator;
            this.scopeId = scopeId;
            this.offset = tempAllocator->current;
        }

        public Span<T> AllocateSpan<T>(int itemCount) where T : unmanaged {
            return new Span<T>(Allocate<T>(itemCount), itemCount);
        }

        public T* Allocate<T>(int itemCount = 1) where T : unmanaged {
            return tempAllocator->Allocate<T>(scopeId, itemCount);
        }

        public ScopedList<T> CreateList<T>(int itemCapacity) where T : unmanaged {
            return tempAllocator->CreateList<T>(this, itemCapacity);
        }

        // implicitly implemented! 
        public void Dispose() {
            tempAllocator->PopScope(this);
            this = default;
        }

    }

    internal unsafe struct LooseScopedList<T> where T : unmanaged {

        public int capacity;
        public int size;
        public T* array;
        public readonly ScopedTempAllocator* tempAllocator;
        public LooseAllocatorScope scope;
        
        public LooseScopedList(ScopedTempAllocator* tempAllocator, T* array, int capacity, LooseAllocatorScope scope) {
            this.scope = scope;
            this.tempAllocator = tempAllocator;
            this.array = array;
            this.capacity = capacity;
            this.size = 0;
        }

        public void Add(in T item) {

            if (size + 1 >= capacity) {
                array = tempAllocator->Reallocate(scope.scopeId, array, capacity, capacity * 2);
                capacity *= 2;
            }

            array[size++] = item;

        }

        public void AddRange(CheckedArray<T> list) {
            if (size + list.size >= capacity) {
                int newCapacity = Math.Max(capacity * 2, list.size * 2 + capacity);
                array = tempAllocator->Reallocate(scope.scopeId, array, capacity, newCapacity);
                capacity = newCapacity;
            }

            Native.Memory.MemCpy(array + size, list.GetArrayPointer(), list.size);
            size += list.size;

        }

        public void SetSize(int size) {
            this.size = size;
        }

        public CheckedArray<T> ToUnsafeCheckedArray() {
            return new CheckedArray<T>(array, size);
        }

        public ref T Get(int index) {
            TypedUnsafe.CheckRange(index, size);
            return ref array[index];
        }

    }

    internal unsafe ref struct ScopedList<T> where T : unmanaged {

        public int capacity;
        public int size;
        public T* array;
        public readonly ScopedTempAllocator* tempAllocator;
        public readonly int scopeId;
        private readonly byte* ownScopeOffset;

        public ScopedList(ScopedTempAllocator* tempAllocator, T* array, int capacity, byte* ownScopeOffset, int scopeId) {
            this.ownScopeOffset = ownScopeOffset;
            this.tempAllocator = tempAllocator;
            this.scopeId = scopeId;
            this.array = array;
            this.capacity = capacity;
            this.size = 0;
        }

        public ScopedList(ScopedTempAllocator* tempAllocator, AllocatorScope scope, T* array, int capacity) {
            this.ownScopeOffset = null;
            this.tempAllocator = tempAllocator;
            this.scopeId = scope.scopeId;
            this.array = array;
            this.capacity = capacity;
            this.size = 0;
        }

        public void Add(in T item) {

            if (size + 1 >= capacity) {
                array = tempAllocator->Reallocate(scopeId, array, capacity, capacity * 2);
                capacity *= 2;
            }

            array[size++] = item;

        }

        public void Dispose() {
            if (ownScopeOffset != null) {
                tempAllocator->PopScope(scopeId, ownScopeOffset);
            }

            this = default;
        }

        public void AddRange(CheckedArray<T> list) {
            if (size + list.size >= capacity) {
                int newCapacity = Math.Max(capacity * 2, list.size * 2 + capacity);
                array = tempAllocator->Reallocate(scopeId, array, capacity, newCapacity);
                capacity = newCapacity;
            }

            Memory.MemCpy(array + size, list.GetArrayPointer(), list.size);
            size += list.size;

        }

        public void SetSize(int size) {
            this.size = size;
        }

        public ref T Get(int index) {
            TypedUnsafe.CheckRange(index, size);
            return ref array[index];
        }

    }

}