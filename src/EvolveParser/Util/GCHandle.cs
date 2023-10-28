using System;
using System.Runtime.InteropServices;

namespace EvolveUI.Util {
    public struct GCHandle<T> : IDisposable where T : class {

        public GCHandle handle;

        public GCHandle(T target) {
            handle = GCHandle.Alloc(target);
        }

        public bool IsAllocated => handle.IsAllocated;

        public T Get() {
            return (T) handle.Target;
        }

        public void Dispose() {
            if (handle.IsAllocated) {
                handle.Free();
            }
        }

    }

}
