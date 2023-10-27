
namespace EvolveUI.Util.Unsafe {

    internal unsafe struct MemoryPool : IDisposable {

        public readonly byte* allocation;
        public readonly long capacityInBytes;

        public MemoryPool(long capacityInBytes) {
            this.allocation = Native.Memory.AlignedMalloc<byte>(capacityInBytes);
            this.capacityInBytes = capacityInBytes;
        }

        public void Dispose() {
            Native.Memory.AlignedFree(allocation, Native.Memory.AlignOf<byte>());
            this = default;
        }

    }

}