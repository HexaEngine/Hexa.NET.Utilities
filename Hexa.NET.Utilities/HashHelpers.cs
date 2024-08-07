namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;

    public class HashHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetFastModMultiplier(uint divisor)
        {
            return ulong.MaxValue / divisor + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastMod(uint value, uint divisor, ulong multiplier)
        {
            ulong lowbits = multiplier * value;
            return (uint)(((lowbits >> 32) + 1) * divisor >> 32);
        }
    }
}