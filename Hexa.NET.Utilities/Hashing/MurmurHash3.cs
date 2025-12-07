namespace Hexa.NET.Utilities.Hashing
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct MurmurHash3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Rotl32(uint x, byte r) => (x << r) | (x >> (32 - r));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl64(ulong x, byte r) => (x << r) | (x >> (64 - r));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Fmix32(uint h)
        {
            unchecked
            {
                h ^= h >> 16;
                h *= 0x85ebca6b;
                h ^= h >> 13;
                h *= 0xc2b2ae35;
                h ^= h >> 16;
                return h;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Fmix64(ulong k)
        {
            unchecked
            {
                k ^= k >> 33;
                k *= 0xff51afd7ed558ccdUL;
                k ^= k >> 33;
                k *= 0xc4ceb9fe1a85ec53UL;
                k ^= k >> 33;
                return k;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUInt32LE(byte* data)
        {
            if (BitConverter.IsLittleEndian)
            {
                return *(uint*)data;
            }
            return (uint)(data[0]
                | (data[1] << 8)
                | (data[2] << 16)
                | (data[3] << 24));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadUInt64LE(byte* data)
        {
            if (BitConverter.IsLittleEndian)
            {
                return *(ulong*)data;
            }
            ulong lo = ReadUInt32LE(data);
            ulong hi = ReadUInt32LE(data + 4);
            return lo | (hi << 32);
        }

        public static void HashCore(byte* data, nuint length, byte* hash, uint seed = 0)
        {
            unchecked
            {
                const ulong c1 = 0x87c37b91114253d5UL;
                const ulong c2 = 0x4cf5ad432745937fUL;

                ulong h1 = seed;
                ulong h2 = seed;

                nuint nblocks = length / 16;

                // body
                for (nuint i = 0; i < nblocks; ++i)
                {
                    nuint offset = i * 16;
                    ulong k1 = ReadUInt64LE(data + offset);
                    ulong k2 = ReadUInt64LE(data + offset + 8);

                    k1 *= c1;
                    k1 = Rotl64(k1, 31);
                    k1 *= c2;
                    h1 ^= k1;
                    h1 = Rotl64(h1, 27);
                    h1 += h2;
                    h1 = h1 * 5 + 0x52dce729;

                    k2 *= c2;
                    k2 = Rotl64(k2, 33);
                    k2 *= c1;
                    h2 ^= k2;
                    h2 = Rotl64(h2, 31);
                    h2 += h1;
                    h2 = h2 * 5 + 0x38495ab5;
                }

                // tail
                nuint tailIndex = nblocks * 16;
                ulong k1Tail = 0;
                ulong k2Tail = 0;
                nuint tailLen = length & 15;

                if (tailLen > 0)
                {
                    // process in little-endian order
                    // we reconstruct k1Tail and k2Tail from remaining bytes
                    for (nuint i = tailLen - 1; i >= 0; --i)
                    {
                        byte b = data[tailIndex + i];
                        if (i >= 8)
                        {
                            k2Tail <<= 8;
                            k2Tail |= b;
                        }
                        else
                        {
                            k1Tail <<= 8;
                            k1Tail |= b;
                        }
                    }

                    // apply mixing for tails
                    if (k2Tail != 0)
                    {
                        k2Tail *= c2;
                        k2Tail = Rotl64(k2Tail, 33);
                        k2Tail *= c1;
                        h2 ^= k2Tail;
                    }

                    if (k1Tail != 0)
                    {
                        k1Tail *= c1;
                        k1Tail = Rotl64(k1Tail, 31);
                        k1Tail *= c2;
                        h1 ^= k1Tail;
                    }
                }

                // finalization
                h1 ^= (ulong)length;
                h2 ^= (ulong)length;

                h1 += h2;
                h2 += h1;

                h1 = Fmix64(h1);
                h2 = Fmix64(h2);

                h1 += h2;
                h2 += h1;

                ulong* hashUlong = (ulong*)hash;
                hashUlong[0] = h1;
                hashUlong[1] = h2;
            }
        }

        public static void Hash(ReadOnlySpan<byte> data, Span<byte> hash, uint seed = 0)
        {
            if (hash.Length < 16) throw new ArgumentException("Hash span must be at least 16 bytes long", nameof(hash));
            fixed (byte* dataPtr = data)
            fixed (byte* hashPtr = hash)
            {
                HashCore(dataPtr, (nuint)data.Length, hashPtr, seed);
            }
        }

        public static void Hash(ReadOnlySpan<char> text, Span<byte> hash, uint seed = 0)
        {
            Hash(MemoryMarshal.AsBytes(text), hash, seed);
        }

        public static ulong Hash64(byte* data, nuint length, uint seed = 0)
        {
            byte* hash = stackalloc byte[16];
            HashCore(data, length, hash, seed);
            var span = (ulong*)hash;
            return span[0] ^ (span[1] * 0x9E3779B97F4A7C15UL);
        }

        public static ulong Hash64(ReadOnlySpan<byte> data, uint seed = 0)
        {
            Span<byte> hash = stackalloc byte[16];
            Hash(data, hash, seed);
            var span = MemoryMarshal.Cast<byte, ulong>(hash);
            return span[0] ^ (span[1] * 0x9E3779B97F4A7C15UL);
        }

        public static ulong Hash64(ReadOnlySpan<char> text, uint seed = 0)
        {
            return Hash64(MemoryMarshal.AsBytes(text), seed);
        }
    }
}