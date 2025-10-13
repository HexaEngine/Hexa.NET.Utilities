namespace Hexa.NET.Utilities
{
#if NET8_0_OR_GREATER
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Intrinsics;
    using System.Runtime.Intrinsics.X86;

    public unsafe struct HashMap<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        public struct Tag
        {
            public const byte FilledFlag = 0x80;
            public const byte FlagMask = unchecked((byte)~FilledFlag);
            public const byte TagMask = 0x7F;
            public const byte TombstoneState = TagMask;
            public const byte EmptyState = 0;

            public byte raw;

            public Tag(byte raw)
            {
                this.raw = raw;
            }

            public static Tag Derive(ulong hash)
            {
                var raw = hash;
                raw *= 0x9E3779B97F4A7C15UL;
                raw ^= raw >> 33;
                raw ^= raw >> 29;
                raw ^= raw >> 27;

                return new Tag((byte)((raw & TagMask) | FilledFlag));
            }

            public byte TagHash
            {
                readonly get => (byte)(raw & TagMask);
                set => raw = (byte)((raw & FlagMask) | (value & TagMask));
            }

            public readonly bool IsEmpty => raw == EmptyState;
            public readonly bool IsFilled => (raw & FilledFlag) == FilledFlag;
            public readonly bool IsTombstone => raw == TombstoneState;

            public void SetEmpty() => raw = EmptyState;

            public void SetFilled() => raw |= FilledFlag;

            public void SetTombstone() => raw = TombstoneState;
        }

        public struct Pair
        {
            public TKey key;
            public TValue value;

            public Pair(in TKey key, in TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        public struct Columns
        {
            public Tag* tags;
            public Pair* pairs;

            public Columns(Tag* tags, Pair* pairs)
            {
                this.tags = tags;
                this.pairs = pairs;
            }

            public readonly bool IsFilled => tags->IsFilled;

            public ref Tag Tag => ref Unsafe.AsRef<Tag>(tags);

            public ref Pair Pair => ref Unsafe.AsRef<Pair>(pairs);

            public static Columns operator +(Columns a, int offset) => new() { tags = a.tags + offset, pairs = a.pairs + offset };

            public static Columns operator +(Columns a, uint offset) => new() { tags = a.tags + offset, pairs = a.pairs + offset };

            public static Columns operator -(Columns a, int offset) => new() { tags = a.tags - offset, pairs = a.pairs - offset };

            public static Columns operator -(Columns a, uint offset) => new() { tags = a.tags - offset, pairs = a.pairs - offset };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Broadcast8To64(byte value)
        {
            return value * 0x0101010101010101UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Match8Bytes(ulong chunk, ulong cmp)
        {
            ulong x = chunk ^ cmp;
            return (x - 0x0101010101010101UL) & ~x & 0x8080808080808080UL;
        }

        private static bool KeyEquals(TKey a, TKey b)
        {
            return EqualityComparer<TKey>.Default.Equals(a, b);
        }

        public static readonly int ProbeWidth = Avx2.IsSupported ? 64 : 8;
        public static readonly int ProbeShift = Avx2.IsSupported ? 0 : 3;

        public static int Probe(TKey key, Tag tag, in Columns cols)
        {
            if (Avx2.IsSupported)
            {
                Vector256<byte> target = Vector256.Create(tag.raw);

                var tags0 = Avx.LoadVector256((byte*)cols.tags);
                var tags1 = Avx.LoadVector256((byte*)cols.tags + 32);
                Vector256<byte> cmp0 = Avx2.CompareEqual(tags0, target);
                Vector256<byte> cmp1 = Avx2.CompareEqual(tags1, target);
                ulong mask0 = (ulong)Avx2.MoveMask(cmp0);
                ulong mask1 = (ulong)Avx2.MoveMask(cmp1);
                ulong mask = mask0 | (mask1 << 32);

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask);
                    if (KeyEquals(key, cols.pairs[bit].key)) { return bit; }
                    mask &= (mask - 1);
                }

                Vector256<byte> tombstone = Vector256.Create(Tag.EmptyState);
                Vector256<byte> empty = Vector256.Create(Tag.TombstoneState);

                ulong candidates = 0;
                candidates |= (ulong)Avx2.MoveMask(Avx2.CompareEqual(tags0, tombstone)) | (ulong)Avx2.MoveMask(Avx2.CompareEqual(tags0, empty));
                candidates |= ((ulong)Avx2.MoveMask(Avx2.CompareEqual(tags1, tombstone)) | (ulong)Avx2.MoveMask(Avx2.CompareEqual(tags1, empty))) << 32;

                if (candidates != 0)
                {
                    return BitOperations.TrailingZeroCount(candidates);
                }

                return -1;
            }
            else
            {
                ulong target = Broadcast8To64(tag.raw);
                ulong k64 = *(ulong*)cols.tags;
                ulong mask = 0;
                mask |= Match8Bytes(k64, target);

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask) >> 3;
                    if (KeyEquals(key, cols.pairs[bit].key)) { return bit; }
                    mask &= mask - 1;
                }

                ulong emptyTarget = Broadcast8To64(Tag.EmptyState);
                ulong tombTarget = Broadcast8To64(Tag.TombstoneState);
                ulong emptyMask = Match8Bytes(k64, emptyTarget);
                ulong tombMask = Match8Bytes(k64, tombTarget);
                ulong available = emptyMask | tombMask;
                if (available != 0)
                {
                    return BitOperations.TrailingZeroCount(available) >> 3;
                }

                return -1;
            }
        }

        public static ulong ProbeNonEmpty(Tag* tags)
        {
            if (Avx2.IsSupported)
            {
                var tags0 = Avx.LoadVector256((byte*)tags);
                var tags1 = Avx.LoadVector256((byte*)tags + 32);

                ulong mask0 = (ulong)Avx2.MoveMask(tags0);
                ulong mask1 = (ulong)Avx2.MoveMask(tags1);

                return mask0 | (mask1 << 32);
            }
            else
            {
                ulong k64 = *(ulong*)tags;
                return k64 & 0x8080808080808080UL;
            }
        }

        public static int Find(int hash, Tag tag, TKey key, in Columns cols, int capacity)
        {
            int startIndex = AlignmentHelper.AlignDown(hash & (capacity - 1), ProbeWidth);

            int index = startIndex;

            do
            {
                var nextIndex = (index + ProbeWidth) & (capacity - 1);
                var result = Probe(key, tag, cols + index);
                if (result != -1)
                {
                    return index + result;
                }

                index = nextIndex;
            } while (index != startIndex);

            // this should never happen only if someone forgot to call reserve.
            throw new Exception("Infinite loop detected in find_entry.");
        }

        private Tag* tags;
        private Pair* pairs;
        private int capacity;
        private int size;
        private nint equals;

        public int Count => size;

        public int Capacity => capacity;

        private static void RehashMoveRow(in Columns src, in Columns dst, int newCapacity)
        {
            var tag = *src.tags;
            ref var key = ref src.pairs->key;
            var hash = key.GetHashCode();

            var index = Find(hash, tag, key, dst, newCapacity);

            dst.tags[index] = tag;
            dst.pairs[index] = *src.pairs;
        }

        private static int NextPowerOf2(int v)
        {
            if (v < 1) return 1;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            return v + 1;
        }

        private void Resize(int newCapacity)
        {
            newCapacity = (int)AlignmentHelper.AlignUp((uint)newCapacity, (uint)ProbeWidth);
            newCapacity = NextPowerOf2(newCapacity);
            var tagsInBytes = (nuint)(newCapacity * sizeof(Tag));
            var newTags = (Tag*)NativeMemory.AlignedAlloc(tagsInBytes, (nuint)ProbeWidth);
            NativeMemory.Clear(newTags, tagsInBytes);

            var pairsInBytes = (nuint)(newCapacity * sizeof(Pair));
            var newPairs = (Pair*)NativeMemory.Alloc(pairsInBytes);

            Columns cols = new(tags, pairs);
            Columns newCols = new(newTags, newPairs);

            for (uint i = 0; i < size; cols += ProbeWidth)
            {
                var probe = ProbeNonEmpty(cols.tags);
                while (probe != 0)
                {
                    var bit = BitOperations.TrailingZeroCount(probe) >> ProbeShift;
                    RehashMoveRow(cols + bit, newCols, newCapacity);
                    probe &= probe - 1;
                    i++;
                }
            }

            NativeMemory.AlignedFree(tags);
            NativeMemory.Free(pairs);
            tags = newTags;
            pairs = newPairs;
            capacity = newCapacity;
        }

        public void EnsureCapacity(int minCapacity)
        {
            if (minCapacity > capacity)
            {
                Resize(Math.Max(capacity * 2, minCapacity));
            }
        }

        public bool Contains(TKey key)
        {
            var hash = key.GetHashCode();
            var tag = Tag.Derive((ulong)hash);
            int index = Find(hash, tag, key, new Columns(tags, pairs), capacity);
            return tags[index].IsFilled;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var hash = key.GetHashCode();
            var tag = Tag.Derive((ulong)hash);
            int index = Find(hash, tag, key, new Columns(tags, pairs), capacity);
            if (tags[index].IsFilled)
            {
                value = pairs[index].value;
                return true;
            }
            value = default;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            int nextSize = size + 1;
            EnsureCapacity(nextSize);
            var hash = key.GetHashCode();
            var tag = Tag.Derive((ulong)hash);
            Columns cols = new(tags, pairs);
            cols += Find(hash, tag, key, cols, capacity);
            if (cols.IsFilled)
            {
                throw new ArgumentException("Key already exists.");
            }

            cols.Tag = tag;
            cols.Pair = new(key, value);
            size = nextSize;
        }

        public bool Remove(TKey key)
        {
            var hash = key.GetHashCode();
            var tag = Tag.Derive((ulong)hash);
            Columns cols = new(tags, pairs);
            cols += Find(hash, tag, key, cols, capacity);
            if (cols.IsFilled)
            {
                cols.Tag.SetTombstone();
                size--;
                return true;
            }
            return false;
        }

        public void Release()
        {
            if (tags != null)
            {
                NativeMemory.AlignedFree(tags); 
                tags = null;
            }

            if (pairs != null)
            {
                NativeMemory.Free(pairs);
                pairs = null;
            }

            capacity = 0;
            size = 0;
        }

        public void Clear()
        {
            if (tags == null)
            {
                return;
            }
            var tagsInBytes = (nuint)(capacity * sizeof(Tag));
            NativeMemory.Clear(tags, tagsInBytes);
            size = 0;
        }
    }

    

#endif
}
