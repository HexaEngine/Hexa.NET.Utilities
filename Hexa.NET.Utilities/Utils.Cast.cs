namespace Hexa.NET.Utilities
{
    public unsafe static partial class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTo BitCast<TFrom, TTo>(TFrom value) where TTo : unmanaged where TFrom : unmanaged
        {
#if NET8_0_OR_GREATER
            return Unsafe.BitCast<TFrom, TTo>(value);
#else
            if (sizeof(TFrom) != sizeof(TTo))
            {
                throw new NotSupportedException("BitCast requires source and destination to be the same size.");
            }

            return Unsafe.As<TFrom, TTo>(ref value);
#endif
        }
    }
}