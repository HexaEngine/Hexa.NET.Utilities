using System.Runtime.InteropServices;

namespace Hexa.NET.Utilities
{
    public unsafe static class GCUtils
    {
        public static void* GCAlloc(object obj)
        {
            GCHandle handle = GCHandle.Alloc(obj);
            return (void*)(nint)handle;
        }

        public static T? GetObjectAs<T>(void* ptr) where T : class
        {
            if (ptr == null)
            {
                return null;
            }
            GCHandle handle = GCHandle.FromIntPtr((nint)ptr);
            return handle.Target as T;
        }

        public static T GetObject<T>(void* ptr) where T : class
        {
            if (ptr == null)
            {
                throw new ArgumentNullException(nameof(ptr));
            }
            GCHandle handle = GCHandle.FromIntPtr((nint)ptr);
            return handle.Target as T ?? throw new InvalidCastException($"Cannot cast object to type {typeof(T).FullName}");
        }

        public static void GCFree(void* ptr)
        {
            if (ptr == null)
            {
                return;
            }
            GCHandle handle = GCHandle.FromIntPtr((nint)ptr);
            handle.Free();
        }
    }
}