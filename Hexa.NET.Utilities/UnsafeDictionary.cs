namespace Hexa.NET.Utilities
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;

    public unsafe struct UnsafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IEquatable<UnsafeDictionary<TKey, TValue>> where TKey : unmanaged where TValue : unmanaged
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
            public TKey Key;
            public TValue Value;
            public EntryFlags Flags;

            public Entry(int hashCode, TKey key, TValue value, EntryFlags flags)
            {
                HashCode = hashCode;
                Key = key;
                Value = value;
                Flags = flags;
            }

            public static Entry Empty => new(0, default, default, EntryFlags.Empty);

            public static Entry Tombstone => new(0, default, default, EntryFlags.Tombstone);

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
        private delegate*<TKey, TKey, bool> comparer;

        private const float loadFactor = 0.75f;

        public UnsafeDictionary(int initialCapacity)
        {
            Capacity = initialCapacity;
            size = 0;
        }

        public UnsafeDictionary(int initialCapacity, delegate*<TKey, TKey, bool> comparer)
        {
            Capacity = initialCapacity;
            size = 0;
            this.comparer = comparer;
        }

        public UnsafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs is UnsafeDictionary<TKey, TValue> dictionary)
            {
                this = dictionary.Clone();
                return;
            }

            if (keyValuePairs is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                Capacity = collection.Count;
            }

            foreach (var pair in keyValuePairs)
            {
                Add(pair.Key, pair.Value);
            }

            TrimExcess();
        }

        public UnsafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs, delegate*<TKey, TKey, bool> comparer)
        {
            this.comparer = comparer;

            if (keyValuePairs is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                Capacity = collection.Count;
            }

            foreach (var pair in keyValuePairs)
            {
                Add(pair.Key, pair.Value);
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
                        Entry* dest = FindEntry(newBuckets, value, entry->Key, entry->HashCode);
                        *dest = *entry;
                    }
                }

                Free(buckets);
                buckets = newBuckets;
                capacity = value;
            }
        }

        private readonly Entry* FindEntry(Entry* entries, int capacity, TKey key, int hashCode)
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
                else if (entry->HashCode == hashCode && Compare(entry->Key, key))
                {
                    // We found the key.
                    return entry;
                }

                index = (index + 1) % capacity;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool Compare(TKey x, TKey y)
        {
            if (comparer == null)
            {
                return EqualityComparer<TKey>.Default.Equals(x, y);
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

        public void Add(TKey key, TValue value)
        {
            EnsureCapacity(size + 1);

            int hashCode = key.GetHashCode();

            Entry* entry = FindEntry(buckets, capacity, key, hashCode);

            if (entry->IsFilled)
            {
                throw new ArgumentException("Key already exists in the dictionary");
            }

            entry->HashCode = hashCode;
            entry->Key = key;
            entry->Value = value;
            entry->Flags = EntryFlags.Filled;

            size++;
        }

        public void Set(TKey key, TValue value)
        {
            EnsureCapacity(size + 1);

            int hashCode = key.GetHashCode();

            Entry* entry = FindEntry(buckets, capacity, key, hashCode);

            bool isNewKey = !entry->IsFilled;

            if (isNewKey)
            {
                size++;
            }

            entry->HashCode = hashCode;
            entry->Key = key;
            entry->Value = value;
            entry->Flags = EntryFlags.Filled;
        }

        public readonly bool ContainsKey(TKey key)
        {
            int hashCode = key.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, key, hashCode);
            return entry->HashCode == hashCode;
        }

        public bool Remove(TKey key)
        {
            if (size == 0)
            {
                return false;
            }

            int hashCode = key.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, key, hashCode);

            if (!entry->IsFilled)
            {
                return false;
            }

            *entry = Entry.Tombstone;
            size--;

            return true;
        }

        public readonly bool TryGetValue(TKey key, out TValue value)
        {
            if (size == 0)
            {
                value = default;
                return false;
            }

            int hashCode = key.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, key, hashCode);

            if (!entry->IsFilled)
            {
                value = default;
                return false;
            }

            value = entry->Value;
            return true;
        }

        public void Clear()
        {
            MemsetT(buckets, Entry.Empty, capacity);
            size = 0;
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                List<TKey> keys = new(capacity);
                for (int i = 0; i < capacity; i++)
                {
                    Entry* current = &buckets[i];
                    if (!current->IsFilled)
                    {
                        continue;
                    }

                    keys.Add(current->Key);
                }
                return keys;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                List<TValue> values = new(capacity);
                for (int i = 0; i < capacity; i++)
                {
                    Entry* current = &buckets[i];
                    if (!current->IsFilled)
                    {
                        continue;
                    }

                    values.Add(current->Value);
                }
                return values;
            }
        }

        public readonly int Size => size;

        public readonly int Count => size;

        public readonly bool IsReadOnly => false;

        public readonly IEnumerable<TKey> Keys => new KeyEnumerator(this);

        public readonly IEnumerable<TValue> Values => new ValueEnumerator(this);

        public TValue this[TKey key]
        {
            readonly get
            {
                if (TryGetValue(key, out TValue value))
                {
                    return value;
                }
                throw new KeyNotFoundException("The given key was not present in the dictionary.");
            }
            set => Set(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public readonly bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetValue(item.Key, out TValue value))
            {
                return EqualityComparer<TValue>.Default.Equals(value, item.Value);
            }
            return false;
        }

        public readonly void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(array.Length - arrayIndex, size);
#else
            if (array == null)
            {
                throw new ArgumentNullException();
            }
            int len = array.Length - arrayIndex;
            if (arrayIndex < 0 || array.Length - arrayIndex >= size)
            {
                throw new ArgumentOutOfRangeException();
            }
#endif

            foreach (var pair in this)
            {
                array[arrayIndex++] = pair;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        private struct KeyValuePairEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private UnsafeDictionary<TKey, TValue> dictionary;
            private int index;
            private int itemIndex;
            private Entry* current;

            public KeyValuePairEnumerator(UnsafeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
                index = 0;
                current = null;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException("The enumerator is positioned before the first element or after the last element.");
                    }
                    return new KeyValuePair<TKey, TValue>(current->Key, current->Value);
                }
            }

            object IEnumerator.Current => Current;

            public readonly void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (itemIndex >= dictionary.capacity - 1)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
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

        private struct KeyEnumerator : IEnumerable<TKey>, IEnumerator<TKey>
        {
            private UnsafeDictionary<TKey, TValue> dictionary;
            private int index;
            private int itemIndex;
            private Entry* current;

            public KeyEnumerator(UnsafeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
                index = 0;
                current = null;
            }

            public TKey Current
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException("The enumerator is positioned before the first element or after the last element.");
                    }
                    return current->Key;
                }
            }

            object IEnumerator.Current => Current;

            public readonly void Dispose()
            {
            }

            public readonly IEnumerator<TKey> GetEnumerator()
            {
                return this;
            }

            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (itemIndex >= dictionary.capacity - 1)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
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

        private struct ValueEnumerator : IEnumerable<TValue>, IEnumerator<TValue>
        {
            private UnsafeDictionary<TKey, TValue> dictionary;
            private int index;
            private int itemIndex;
            private Entry* current;

            public ValueEnumerator(UnsafeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
                index = 0;
                current = null;
            }

            public TValue Current
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

            public readonly IEnumerator<TValue> GetEnumerator()
            {
                return this;
            }

            readonly IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (itemIndex >= dictionary.capacity - 1)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
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

        public readonly IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new KeyValuePairEnumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is UnsafeDictionary<TKey, TValue> dictionary && Equals(dictionary);
        }

        public readonly bool Equals(UnsafeDictionary<TKey, TValue> other)
        {
            return (buckets == other.buckets);
        }

        public override readonly int GetHashCode()
        {
            return ((nint)buckets).GetHashCode();
        }

        public static bool operator ==(UnsafeDictionary<TKey, TValue> left, UnsafeDictionary<TKey, TValue> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeDictionary<TKey, TValue> left, UnsafeDictionary<TKey, TValue> right)
        {
            return !(left == right);
        }

        public static bool operator ==(IEnumerable<KeyValuePair<TKey, TValue>> left, UnsafeDictionary<TKey, TValue> right)
        {
            return left is UnsafeDictionary<TKey, TValue> set && set == right;
        }

        public static bool operator !=(IEnumerable<KeyValuePair<TKey, TValue>> left, UnsafeDictionary<TKey, TValue> right)
        {
            return !(left == right);
        }

        public readonly UnsafeDictionary<TKey, TValue> Clone()
        {
            UnsafeDictionary<TKey, TValue> result;
            result.capacity = capacity;
            result.buckets = AllocT<Entry>(capacity);
            result.size = size;
            result.comparer = comparer;
            MemcpyT(buckets, result.buckets, capacity);
            return result;
        }
    }
}