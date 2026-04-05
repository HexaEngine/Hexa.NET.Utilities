using System.Runtime.CompilerServices;

namespace Hexa.NET.Utilities
{
    public unsafe static partial class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AlignedAlloc(nuint size, nuint alignment)
        {
            return NativeMemory.AlignedAlloc(size, alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AlignedFree(void* ptr)
        {
            NativeMemory.AlignedFree(ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AlignedFree<T>(T* ptr) where T : unmanaged, IFreeable
        {
            ptr->Release();
            NativeMemory.AlignedFree(ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(nuint count, nuint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)sizeof(T) * count, alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(nint count, nuint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)(sizeof(T) * count), alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(uint count, nuint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)sizeof(T) * count, alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(int count, nuint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)(sizeof(T) * count), alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(nuint count, nint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)sizeof(T) * count, (nuint)alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(nint count, uint alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)(sizeof(T) * count), alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AlignedAllocT<T>(uint count, int alignment) where T : unmanaged
        {
            return (T*)AlignedAlloc((nuint)sizeof(T) * count, (nuint)alignment);
        }
    }
}