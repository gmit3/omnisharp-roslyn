using EvolveUI.Util;
using EvolveUI.Util.Unsafe;

namespace EvolveUI.Parsing {
    public unsafe struct OffsetRange<T> where T : unmanaged {

        public int offsetInBytes;
        public int countInItems;

        public static OffsetRange<T> Create(PodList<byte> * bytes, CheckedArray<T> data) {
            int sizeInBytes = sizeof(T) * data.size;
            int alignedSize = Align8(sizeInBytes);
            bytes->EnsureAdditionalCapacity(alignedSize);
            int offset = bytes->size;

            byte* allocPtr = bytes->GetArrayPointer() + bytes->size;

            // we bump the pointer in such a way that each time we write into the buffer
            // the size of the buffer gets adjusted to the aligned size of the last write 

            bytes->size += alignedSize;

            if (((ulong)allocPtr & 7) != 0) {
                LogUtil.Error("Pointer is not 8 byte aligned");
            }

            Native.Memory.MemCpy(allocPtr, (byte*)data.GetArrayPointer(), sizeInBytes);

            return new OffsetRange<T>() {
                offsetInBytes = offset,
                countInItems = data.size
            };
        }

        private static int Align8(int number) {
            return ((number + 7) & -8);
        }

        public CheckedArray<T> FromBytes(CheckedArray<byte> bytes) {
            return new CheckedArray<T>(bytes.GetArrayPointer() + offsetInBytes, countInItems);
        }

    }

}
