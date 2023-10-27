using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EvolveUI.Util.Unsafe {

    internal enum AllocatorUShort : ushort {

        Invalid,
        None,
        Temp,
        TempJob,
        Persistent,
        AudioKernel,

    }

    internal static unsafe class TypedUnsafe {

        public const int k_Kilobyte = 1024;
        public const int k_Megabyte = 1048576;
        public const int k_Gigabyte = 1073741824;

        public static int Megabytes(int count) {
            return count * 1024 * 1024;
        }

        public static int Kilobytes(int count) {
            return count * 1024;
        }

        // public static void MemClear<T>(T* ptr, long itemCount) where T : unmanaged {
        //     if (ptr == null || itemCount <= 0) return;
        //     UnsafeUtility.MemClear(ptr, itemCount * sizeof(T));
        // }
        //
        // public static void MemSet<T>(T* ptr, int itemCount, byte value) where T : unmanaged {
        //     if (ptr == null || itemCount <= 0) return;
        //     UnsafeUtility.MemSet(ptr, value, itemCount * sizeof(T));
        // }

        [DebuggerStepThrough]
        [Conditional("DEBUG")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckRange(int index, int size) {
            if ((uint)index >= size) {
                throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)size}' size.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void LessThanZero(int index) {
            if (index < 0) {
                throw new IndexOutOfRangeException($"Index {index} is less than zero");
            }
        }

        [Conditional("DEBUG")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckRange(uint index, int size) {
            if ((uint)index >= size) {
                throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)size}' size.");
            }
        }

        [Conditional("DEBUG")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckSizesEqual(int a, int b) {
            if (a != b) {
                throw new Exception($"Sizes are not equal `{a} != {b}`, failing memory operation");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckSizesAtLeastOrEqual(int a, int b) {
            if (!(b <= a)) {
                throw new Exception($"Sizes are not less than or equal `{a} != {b}`, failing memory operation");
            }
        }

        // public static AllocatedArray<T> AllocateArray<T>(int itemCount, Allocator allocatorLifeTime) where T : unmanaged {
        //     return new AllocatedArray<T>(MallocCleared<T>(itemCount, allocatorLifeTime), itemCount, allocatorLifeTime);
        // }
        //
        // public static AllocatedArray<T> AllocateArrayUncleared<T>(int itemCount, Allocator allocatorLifeTime) where T : unmanaged {
        //     return new AllocatedArray<T>(Malloc<T>(itemCount, allocatorLifeTime), itemCount, allocatorLifeTime);
        // }


        public static long Align(long number, int alignment) {
            return ((number + (alignment - 1)) & (-alignment));
        }

        // public static void SafeBlitStruct<TDest, TSrc>(ref TDest dest, ref TSrc src) where TDest : unmanaged where TSrc : unmanaged {
        //     if (sizeof(TSrc) > sizeof(TDest)) {
        //         throw new Exception($"Cannot blit from {typeof(TSrc).GetTypeName()} into {typeof(TDest).GetTypeName()} because the source is larger than the destination");
        //     }
        //
        //     TSrc* blit = (TSrc*)UnsafeUtility.AddressOf(ref dest);
        //     *blit = default; // memset 0
        //     *blit = src;
        // }
        //
        // public static void SafeBlitStruct<TDest, TSrc>(ref TDest dest, TSrc src) where TDest : unmanaged where TSrc : unmanaged {
        //     if (sizeof(TSrc) > sizeof(TDest)) {
        //         throw new Exception($"Cannot blit from {typeof(TSrc).GetTypeName()} into {typeof(TDest).GetTypeName()} because the source is larger than the destination");
        //     }
        //
        //     TSrc* blit = (TSrc*)UnsafeUtility.AddressOf(ref dest);
        //     *blit = default; // memset 0
        //     *blit = src;
        // }
        //
        // public static TDest SafeReinterpretCopy<TDest, TSrc>(ref TSrc src) where TDest : unmanaged where TSrc : unmanaged {
        //     if (sizeof(TSrc) > sizeof(TDest)) {
        //         throw new Exception($"Cannot blit from {typeof(TSrc).GetTypeName()} into {typeof(TDest).GetTypeName()} because the source is larger than the destination");
        //     }
        //
        //     TDest retn = default;
        //     TSrc* retnSrc = (TSrc*)&retn;
        //     *retnSrc = src;
        //     return retn;
        // }
        //
        // public static ref TOutput Reinterpret<TInput, TOutput>(ref TInput input) where TInput : unmanaged where TOutput : unmanaged {
        //     if (sizeof(TOutput) < sizeof(TInput)) {
        //         throw new Exception($"Cannot reinterpret from {typeof(TInput).GetTypeName()} into {typeof(TOutput).GetTypeName()} because the source is larger than the destination");
        //     }
        //
        //     return ref *(TOutput*)UnsafeUtility.AddressOf(ref input);
        // }
        //
        // public static TOutput Reinterpret<TInput, TOutput>(TInput input) where TInput : unmanaged where TOutput : unmanaged {
        //     if (sizeof(TOutput) < sizeof(TInput)) {
        //         throw new Exception($"Cannot reinterpret from {typeof(TInput).GetTypeName()} into {typeof(TOutput).GetTypeName()} because the source is larger than the destination");
        //     }
        //
        //     return *(TOutput*)UnsafeUtility.AddressOf(ref input);
        // }
        //
        // public static ref TOutput Unpack<TInput, TOutput>(ref TInput input) where TInput : unmanaged where TOutput : unmanaged {
        //     if (sizeof(TOutput) > sizeof(TInput)) {
        //         throw new Exception($"Cannot unpack {typeof(TOutput).GetTypeName()} from {typeof(TInput).GetTypeName()} because the output is larger than the input");
        //     }
        //
        //     return ref *(TOutput*)UnsafeUtility.AddressOf(ref input);
        // }
        //
        // public static ref TOutput Unpack<TInput, TOutput>(TInput input) where TInput : unmanaged where TOutput : unmanaged {
        //     if (sizeof(TOutput) > sizeof(TInput)) {
        //         throw new Exception($"Cannot unpack {typeof(TOutput).GetTypeName()} from {typeof(TInput).GetTypeName()} because the output is larger than the input");
        //     }
        //
        //     return ref *(TOutput*)UnsafeUtility.AddressOf(ref input);
        // }
        //
        // public static void CopyString(char* ptr, string styleName) {
        //     fixed (char* cbuffer = styleName) {
        //         MemCpy(ptr, cbuffer, styleName.Length);
        //     }
        // }
        //
        // public static int Align8(int number) {
        //     return ((number + 7) & -8);
        // }
        //
        // public static int Align16(int number) {
        //     return (number + 15) & (-16);
        // }
        //
        // public static long AlignedNearest16<T>(int number) where T : unmanaged {
        //     return ((number + 15) & -16) * sizeof(T);
        // }
        //
        // public static void Resize<T>(ref AllocatedArray<T> array, int newCapacity, Allocator allocator) where T : unmanaged {
        //     AllocatedArray<T> alloc = AllocateArray<T>(newCapacity, allocator);
        //
        //     if (array.size > 0) {
        //         MemCpy(alloc.GetArrayPointer(), array.GetArrayPointer(), array.size);
        //     }
        //
        //     array.Dispose();
        //     array = alloc;
        // }

    }

}