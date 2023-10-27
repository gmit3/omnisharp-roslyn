// #define EVOLVE_UI_MEMORY_PARANOID

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if UNITY_64
using Unity.Collections.LowLevel.Unsafe;
#endif

#if EVOLVE_UI_MEMORY_PARANOID 

using System;
using EvolveUI.Util.Unsafe;

namespace EvolveUI {

    public static unsafe class ElectricAllocator {

        [Flags]
        public enum AllocationType {

            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000

        }

        [Flags]
        public enum MemoryProtection {

            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400

        }

        [DllImport("kernel32.dll")]
        // private static extern byte* VirtualAlloc(byte* lpAddress, UIntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, AllocationType lAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll")]
        static extern bool VirtualFree(IntPtr lpAddress, long dwSize, long dwFreeType);

        private const int kPageSize = 4096;
        private const int kMemDecommit = 0x00004000;


        public static byte* Allocate(int size) {
            size *= 2;
            int remaining = size % kPageSize;
            int sizeInPages = (size / kPageSize) + (remaining == 0 ? 0 : 1);

            int allocatedSize = ((sizeInPages + 2) * kPageSize);
            byte* virtualAlloc = (byte*)VirtualAlloc(IntPtr.Zero, (UIntPtr)allocatedSize, AllocationType.Commit, MemoryProtection.ReadWrite);

            if (virtualAlloc == null) {
                throw new Exception();
            }

            if (!VirtualProtect(new IntPtr(virtualAlloc), (UIntPtr)kPageSize, (uint)MemoryProtection.NoAccess, out uint _)) {
                throw new Exception();
            }
            
            if (!VirtualProtect(new IntPtr(virtualAlloc + (sizeInPages + 1) * kPageSize), (UIntPtr)kPageSize, (uint)MemoryProtection.NoAccess, out uint _)) {
                throw new Exception();
            }

            byte* firstWritablePage = virtualAlloc + kPageSize;
        

            // write out 0xED as an indication that this memory region is protected
            // technically we only care about the remaining space 
            //Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemSet(firstWritablePage, 0xED, kPageSize * sizeInPages); // don't assume zero'ed mem
            NativeMemory.Fill(firstWritablePage, (UIntPtr)(kPageSize * sizeInPages), 0xED); // don't assume zero'ed mem

            // long* sizeLocation = (long*)firstWritablePage;
            // *sizeLocation = sizeInPages;
                        
            if (remaining == 0) {
                return firstWritablePage;
            }

            // give the memory out so its end would be at the 2nd guard page
            return firstWritablePage + (kPageSize - remaining);
        }

        public static void Free(void* ptr) {
            byte* p = (byte*)ptr;
            int remaining = (int)((long)p % kPageSize);
            byte* firstWritablePage = p - remaining;
            for (int i = 0; i < remaining; i++) {
                if (firstWritablePage[i] != 0xED) {
                    throw new InvalidOperationException("Invalid memory usage!");
                }
            }

            byte* address = firstWritablePage - kPageSize;
            // this will access the memory, which will error if this was already freed
            // if (!VirtualProtect(new IntPtr(address), (UIntPtr)kPageSize, (uint)MemoryProtection.ReadWrite, out uint _)) {
            //     throw new Exception("Electric Memory Write Error");
            // }

            // int dwSize = *(int*)address;

            // decommit, not release, they are not available for reuse again, any 
            // future access will throw
            // if (!VirtualFree(new IntPtr(address), dwSize, kMemDecommit)) {
            //     throw new Exception("Electric Memory Write Error");
            // }
        }

    }

}
#endif

namespace EvolveUI.Native {

    public static unsafe class Memory {

#if UNITY_64
        public static void* AlignedMalloc(long newCapacity, long alignment) {
            return NativeInterface.MallocByteArray(newCapacity, alignment);
        }

        public static void AlignedFree(void* ptr, long alignment) {
            NativeInterface.FreeByteArray(ptr, alignment);
        }

        public static void MemCpy(void* dst, void* src, long size) {
            UnsafeUtility.MemCpy(dst, src, size);
        }

        public static bool MemCmp(void* ptr, void* otherPtr, long size) {
            return UnsafeUtility.MemCmp(ptr, otherPtr, size) == 0;
        }

#else
        
        public static bool MemCmp(void* ptr, void* otherPtr, long size) {
            return new ReadOnlySpan<byte>(ptr, (int)size).SequenceEqual(new ReadOnlySpan<byte>(otherPtr, (int)size));
        }

        public static void MemClear(void* ptr, long size) {
            // NativeMemory.Clear(ptr, (UIntPtr)size);
            if(size > 0)
                Unsafe.InitBlockUnaligned(ptr, 0, (uint)size);
        }

        public static void* AlignedMalloc(long size, long alignment) {
            // return ElectricAllocator.Allocate((int)size);
            return NativeMemory.AlignedAlloc((nuint)size, (nuint)alignment);
        }

        public static void MemCpy(void* dst, void* src, long size) {
            // note the argument order is reversed!
            // NativeMemory.Copy(src, dst, (nuint)size);
            if(size > 0)
                Unsafe.CopyBlockUnaligned(dst, src, (uint)size);
        }

        public static void AlignedFree(void* ptr, long alignment) {
            if (ptr != null) {
                // ElectricAllocator.Free(ptr);
                NativeMemory.AlignedFree(ptr);
            }
        }



#endif

        public static T* AlignedMalloc<T>(long count) where T : unmanaged {
            return (T*)AlignedMalloc(count * sizeof(T), sizeof(AlignOfHelper<T>) - sizeof(T));
        }

        public static void MemCpy<T>(T* dst, T* src, long size) where T : unmanaged {
            MemCpy((void*)dst, src, size * sizeof(T));
        }

        public static int AlignOf<T>() where T : unmanaged => sizeof(AlignOfHelper<T>) - sizeof(T);

        [StructLayout(LayoutKind.Sequential)]
        private struct AlignOfHelper<T> where T : unmanaged {

            public byte dummy;
            public T data;

        }

        public static void MemClear<T>(T* array, int size) where T : unmanaged {
            MemClear((void*)array, size * sizeof(T));
        }

    }

}
