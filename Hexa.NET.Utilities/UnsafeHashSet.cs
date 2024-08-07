namespace Hexa.NET.Utilities
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public unsafe struct UnsafeHashSet<T> : ICollection<T>, ISet<T>, IReadOnlyCollection<T>
#if NET5_0_OR_GREATER
        , IReadOnlySet<T>, IEquatable<UnsafeHashSet<T>>
#endif
        where T : unmanaged
    {
        private enum EntryFlags : byte
        {
            Empty = 0,
            Tombstone = 1,
            Filled = 2
        }

        private struct Entry
        {
            public int HashCode;
            public T Value;
            public EntryFlags Flags;

            public Entry(int hashCode, T value, EntryFlags flags)
            {
                HashCode = hashCode;
                Value = value;
                Flags = flags;
            }

            public static Entry Empty => new(0, default, EntryFlags.Empty);

            public static Entry Tombstone => new(0, default, EntryFlags.Tombstone);

            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return Flags == EntryFlags.Empty;
                }
            }

            public readonly bool IsTombstone
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return Flags == EntryFlags.Tombstone;
                }
            }

            public readonly bool IsFilled
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return Flags == EntryFlags.Filled;
                }
            }
        }

        private Entry* buckets;
        private int capacity;
        private int size;
        private delegate*<T, T, bool> comparer;

        private const float loadFactor = 0.75f;

        public UnsafeHashSet(int initialCapacity)
        {
            Capacity = initialCapacity;
            size = 0;
        }

        public UnsafeHashSet(IEnumerable<T> collection)
        {
            size = 0;

            if (collection is UnsafeHashSet<T> set)
            {
                this = set.Clone();
                return;
            }

            if (collection is ICollection<T> coll)
            {
                Capacity = coll.Count;
            }

            foreach (T item in collection)
            {
                Add(item);
            }

            TrimExcess();
        }

        public UnsafeHashSet(int initialCapacity, delegate*<T, T, bool> comparer)
        {
            Capacity = initialCapacity;
            size = 0;
            this.comparer = comparer;
        }

        public UnsafeHashSet(IEnumerable<T> collection, delegate*<T, T, bool> comparer)
        {
            Capacity = 32; // Default capacity
            size = 0;
            this.comparer = comparer;

            if (collection is UnsafeHashSet<T> set)
            {
                this = set.Clone();
                return;
            }

            if (collection is ICollection<T> coll)
            {
                Capacity = coll.Count;
            }

            foreach (T item in collection)
            {
                Add(item);
            }

            TrimExcess();
        }

        public void Release()
        {
            if (buckets != null)
            {
                Free(buckets);
                this = default;
            }
        }

        public int Capacity
        {
            readonly get => capacity;
            set
            {
                if (value < size)
                {
                    throw new InvalidOperationException("Cannot reduce capacity below current size");
                }

                if (value == capacity)
                {
                    return;
                }

                Entry* newBuckets = AllocT<Entry>(value);
                MemsetT(newBuckets, Entry.Empty, value);

                if (size > 0)
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        Entry* entry = &buckets[i];
                        if (!entry->IsFilled) continue;
                        Entry* dest = FindEntry(newBuckets, value, entry->Value, entry->HashCode);
                        *dest = *entry;
                    }
                }

                Free(buckets);
                buckets = newBuckets;
                capacity = value;
            }
        }

        public readonly int Size => size;

        public readonly int Count => size;

        public readonly bool IsReadOnly => false;

        private readonly Entry* FindEntry(Entry* entries, int capacity, T value, int hashCode)
        {
            Entry* tombstone = null;
            int index = hashCode % capacity;
            while (true)
            {
                Entry* entry = &entries[index];
                if (!entry->IsFilled)
                {
                    if (entry->IsEmpty)
                    {
                        // Empty entry.
                        return tombstone != null ? tombstone : entry;
                    }
                    else
                    {
                        // We found a tombstone.
                        if (tombstone == null)
                        {
                            tombstone = entry;
                        }
                    }
                }
                else if (entry->HashCode == hashCode && Compare(entry->Value, value))
                {
                    // We found the key.
                    return entry;
                }

                index = (index + 1) % capacity;
            }
        }

        private readonly int FindItemIndex(T item)
        {
            int hashCode = item.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, item, hashCode);
            return entry->IsFilled ? (int)(entry - buckets) : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool Compare(T x, T y)
        {
            if (comparer == null)
            {
                return EqualityComparer<T>.Default.Equals(x, y);
            }
            return comparer(x, y);
        }

        public void Grow(int capacity)
        {
            Capacity = capacity * 2;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > this.capacity * loadFactor)
            {
                Grow(capacity);
            }
        }

        public void TrimExcess()
        {
            Capacity = size;
        }

        public bool Add(T value)
        {
            EnsureCapacity(size + 1);

            int hashCode = value.GetHashCode();

            Entry* entry = FindEntry(buckets, capacity, value, hashCode);

            if (entry->IsFilled)
            {
                return false;
            }

            entry->HashCode = hashCode;
            entry->Value = value;
            entry->Flags = EntryFlags.Filled;

            size++;
            return true;
        }

        public bool AddIfNotPresent(T value, out int location)
        {
            EnsureCapacity(size + 1);

            int hashCode = value.GetHashCode();

            Entry* entry = FindEntry(buckets, capacity, value, hashCode);
            location = (int)(entry - buckets);

            if (entry->IsFilled)
            {
                return false;
            }

            entry->HashCode = hashCode;
            entry->Value = value;
            entry->Flags = EntryFlags.Filled;

            size++;
            return true;
        }

        public readonly bool Contains(T value)
        {
            if (size == 0)
            {
                return false;
            }
            int hashCode = value.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, value, hashCode);
            return entry->HashCode == hashCode;
        }

        public bool Remove(T value)
        {
            if (size == 0)
            {
                return false;
            }

            int hashCode = value.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, value, hashCode);

            if (!entry->IsFilled)
            {
                return false;
            }

            *entry = Entry.Tombstone;
            size--;

            return false;
        }

        public void Clear()
        {
            MemsetT(buckets, Entry.Empty, capacity);
            size = 0;
        }

        public void CopyTo(T[] array) => CopyTo(array, 0, Count);

        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex, Count);

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
#if NET5_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
#else
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
#endif
            // Will the array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                throw new ArgumentException("Array is too small");
            }

            for (int i = 0; i < capacity && count != 0; i++)
            {
                Entry* entry = &buckets[i];
                if (entry->IsFilled)
                {
                    array[arrayIndex++] = entry->Value;
                    count--;
                }
            }
        }

        public readonly IEnumerator<T> GetEnumerator()
        {
            return new Enumerator();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                Add(item);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (size == 0 || other == this)
            {
                return;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                if (otherAsCollection.Count == 0)
                {
                    Clear();
                    return;
                }
            }

            IntersectWithEnumerable(other);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (size == 0)
            {
                return;
            }

            if (other == this)
            {
                Clear();
                return;
            }

            foreach (var item in other)
            {
                Remove(item);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            // If set is empty, then symmetric difference is other.
            if (size == 0)
            {
                UnionWith(other);
                return;
            }

            // Special-case this; the symmetric difference of a set with itself is the empty set.
            if (other == this)
            {
                Clear();
                return;
            }

            SymmetricExceptWithEnumerable(other);
        }

        public readonly bool IsSubsetOf(IEnumerable<T> other)
        {
            // The empty set is a subset of any set, and a set is a subset of itself.
            // Set is always a subset of itself
            if (Count == 0 || other == this)
            {
                return true;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount >= 0;
        }

        public readonly bool IsProperSubsetOf(IEnumerable<T> other)
        {
            // No set is a proper subset of itself.
            if (other == this)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // No set is a proper subset of an empty set.
                if (otherAsCollection.Count == 0)
                {
                    return false;
                }

                // The empty set is a proper subset of anything but the empty set.
                if (Count == 0)
                {
                    return otherAsCollection.Count > 0;
                }
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        public readonly unsafe bool IsSupersetOf(IEnumerable<T> other)
        {
            // No set is a proper subset of itself.
            if (other == this)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // No set is a proper subset of an empty set.
                if (otherAsCollection.Count == 0)
                {
                    return false;
                }

                // The empty set is a proper subset of anything but the empty set.
                if (Count == 0)
                {
                    return otherAsCollection.Count > 0;
                }
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        public readonly bool IsProperSupersetOf(IEnumerable<T> other)
        {
            // The empty set isn't a proper superset of any set, and a set is never a strict superset of itself.
            if (Count == 0 || other == this)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    // Note that this has at least one element, based on above check.
                    return true;
                }
            }

            // Couldn't fall out in the above cases; do it the long way
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount < Count && unfoundCount == 0;
        }

        public readonly bool Overlaps(IEnumerable<T> other)
        {
            if (Count == 0)
            {
                return false;
            }

            // Set overlaps itself
            if (other == this)
            {
                return true;
            }

            foreach (T element in other)
            {
                if (Contains(element))
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool SetEquals(IEnumerable<T> other)
        {
            // A set is equal to itself.
            if (other == this)
            {
                return true;
            }

            // If this count is 0 but other contains at least one element, they can't be equal.
            if (Count == 0 &&
                other is ICollection<T> otherAsCollection &&
                otherAsCollection.Count > 0)
            {
                return false;
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount == Count && unfoundCount == 0;
        }

        private unsafe void IntersectWithEnumerable(IEnumerable<T> other)
        {
            BitHelper temp;

            int bytesCount = BitHelper.ToByteArrayLength(size);

            if (bytesCount < StackAllocLimit)
            {
                byte* stackBuffer = stackalloc byte[bytesCount];
                temp = new(stackBuffer, bytesCount);
            }
            else
            {
                temp = new(bytesCount);
            }

            foreach (var item in other)
            {
                int index = FindItemIndex(item);
                if (index >= 0)
                {
                    temp.MarkBit(index);
                }
            }

            int _size = size;
            for (int i = 0; i < _size; i++)
            {
                Entry* entry = &buckets[i];
                if (entry->IsFilled && !temp.IsMarked(i))
                {
                    *entry = Entry.Tombstone; // This is generally faster than calling Remove, because we avoid a hash table lookup.
                    size--;
                }
            }

            temp.Release();
        }

        private unsafe void SymmetricExceptWithEnumerable(IEnumerable<T> other)
        {
            int originalCount = size;
            int intArrayLength = BitHelper.ToByteArrayLength(originalCount);

            BitHelper itemsToRemove;
            BitHelper itemsAddedFromOther;
            if (intArrayLength < StackAllocLimit / 2)
            {
                byte* stackBuffer = stackalloc byte[intArrayLength];
                itemsToRemove = new(stackBuffer, intArrayLength);
                byte* stackBuffer2 = stackalloc byte[intArrayLength];
                itemsAddedFromOther = new(stackBuffer2, intArrayLength);
            }
            else
            {
                itemsToRemove = new(intArrayLength);
                itemsAddedFromOther = new(intArrayLength);
            }

            foreach (T item in other)
            {
                int location;
                if (AddIfNotPresent(item, out location))
                {
                    // wasn't already present in collection; flag it as something not to remove
                    // *NOTE* if location is out of range, we should ignore. BitHelper will
                    // detect that it's out of bounds and not try to mark it. But it's
                    // expected that location could be out of bounds because adding the item
                    // will increase _lastIndex as soon as all the free spots are filled.
                    itemsAddedFromOther.MarkBit(location);
                }
                else
                {
                    // already there...if not added from other, mark for remove.
                    // *NOTE* Even though BitHelper will check that location is in range, we want
                    // to check here. There's no point in checking items beyond originalCount
                    // because they could not have been in the original collection
                    if (location < originalCount && !itemsAddedFromOther.IsMarked(location))
                    {
                        itemsToRemove.MarkBit(location);
                    }
                }
            }

            // if anything marked, remove it
            for (int i = 0; i < originalCount; i++)
            {
                if (itemsToRemove.IsMarked(i))
                {
                    Entry* entry = &buckets[i];
                    *entry = Entry.Tombstone;
                    size--;
                }
            }
        }

        private readonly (int UniqueCount, int UnfoundCount) CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
        {
            // Need special case in case this has no elements.
            if (size == 0)
            {
                int numElementsInOther = 0;
                foreach (T item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return (UniqueCount: 0, UnfoundCount: numElementsInOther);
            }

            int originalCount = size;
            int intArrayLength = BitHelper.ToByteArrayLength(originalCount);

            BitHelper bitHelper;
            if (intArrayLength < StackAllocLimit)
            {
                // Use stackalloc for small int arrays that are less than 2K bytes
                byte* bytes = stackalloc byte[intArrayLength];
                bitHelper = new BitHelper(bytes, intArrayLength);
            }
            else
            {
                // Otherwise, use a heap-allocated BitHelper
                bitHelper = new BitHelper(intArrayLength);
            }

            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this

            foreach (T item in other)
            {
                int index = FindItemIndex(item);
                if (index >= 0)
                {
                    if (!bitHelper.IsMarked(index))
                    {
                        // Item hasn't been seen yet.
                        bitHelper.MarkBit(index);
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            bitHelper.Release(); // cleanup the BitHelper if it was heap-allocated

            return (uniqueFoundCount, unfoundCount);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is UnsafeHashSet<T> set && Equals(set);
        }

        public readonly bool Equals(UnsafeHashSet<T> other)
        {
            return buckets == other.buckets;
        }

        public override readonly int GetHashCode()
        {
            return ((nint)buckets).GetHashCode();
        }

        private struct Enumerator : IEnumerable<T>, IEnumerator<T>
        {
            private UnsafeHashSet<T> hashSet;
            private int index;
            private int itemIndex;
            private Entry* current;

            public Enumerator(UnsafeHashSet<T> hashSet)
            {
                this.hashSet = hashSet;
                index = 0;
                current = null;
            }

            public T Current
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException("The enumerator is positioned before the first element or after the last element.");
                    }
                    return current->Value;
                }
            }

            object IEnumerator.Current => Current;

            public readonly void Dispose()
            {
            }

            public readonly IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (itemIndex >= hashSet.capacity)
                {
                    return false;
                }

                for (int i = index; i < hashSet.capacity; i++)
                {
                    Entry* entry = &hashSet.buckets[i];
                    if (!entry->IsFilled)
                    {
                        continue;
                    }

                    current = entry;
                    index = i + 1;
                    itemIndex++;
                    return true;
                }
                current = null;
                return false;
            }

            public void Reset()
            {
                index = 0;
                itemIndex = 0;
                current = null;
            }
        }

        public static bool operator ==(UnsafeHashSet<T> left, UnsafeHashSet<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeHashSet<T> left, UnsafeHashSet<T> right)
        {
            return !(left == right);
        }

        public static bool operator ==(IEnumerable<T> left, UnsafeHashSet<T> right)
        {
            return left is UnsafeHashSet<T> set && set == right;
        }

        public static bool operator !=(IEnumerable<T> left, UnsafeHashSet<T> right)
        {
            return !(left == right);
        }

        public readonly UnsafeHashSet<T> Clone()
        {
            UnsafeHashSet<T> result;
            result.capacity = capacity;
            result.buckets = AllocT<Entry>(capacity);
            result.size = size;
            result.comparer = comparer;
            MemcpyT(buckets, result.buckets, capacity);
            return result;
        }
    }
}