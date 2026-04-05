namespace Hexa.NET.Utilities
{
#if NETSTANDARD

    using System.Runtime.InteropServices;

    public static unsafe class NativeMemory
    {
        private static readonly delegate*<nuint, nuint, void*> _alloc;
        private static readonly delegate*<void*, void> _free;

        static NativeMemory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _alloc = &Windows._aligned_malloc;
                _free = &Windows._aligned_free;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _alloc = &MacOS.Alloc;
                _free = &MacOS.Free;
            }
            else
            {
                _alloc = &Unix.Alloc;
                _free = &Unix.Free;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AlignedAlloc(nuint size, nuint alignment)
        {
            return _alloc(size, alignment);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AlignedFree(void* ptr)
        {
            _free(ptr);
        }

        private static class Windows
        {
            [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void* _aligned_malloc(nuint size, nuint alignment);

            [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void _aligned_free(void* ptr);
        }

        private static class Unix
        {
            // Linux (glibc, musl)
            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "posix_memalign")]
            private static extern int posix_memalign_libc(void** memptr, nuint alignment, nuint size);

            [DllImport("libc", CallingConvention = CallingConvention.Cdecl, EntryPoint = "free")]
            private static extern void free_libc(void* ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void* Alloc(nuint size, nuint alignment)
            {
                void* ptr;
                if (posix_memalign_libc(&ptr, alignment, size) != 0)
                {
                    return null;
                }

                return ptr;
            }

            internal static void Free(void* ptr) => free_libc(ptr);
        }

        private class MacOS
        {
            // macOS
            [DllImport("/usr/lib/libSystem.B.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "posix_memalign")]
            private static extern int posix_memalign_libSystem(void** memptr, nuint alignment, nuint size);

            [DllImport("/usr/lib/libSystem.B.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "free")]
            private static extern void free_libSystem(void* ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void* Alloc(nuint size, nuint alignment)
            {
                void* ptr;
                if (posix_memalign_libSystem(&ptr, alignment, size) != 0)
                {
                    return null;
                }

                return ptr;
            }

            internal static void Free(void* ptr) => free_libSystem(ptr);
        }
    }

#endif
}