namespace HexaEngine.Unsafes
{
    using System.Collections;

    public unsafe struct UnsafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        private struct Entry
        {
            public int HashCode;
            public TKey Key;
            public TValue Value;

            public static readonly Entry Empty = new() { HashCode = EmptyHashCode };
            public static readonly Entry Tombstone = new() { HashCode = TombstoneHashCode };
        }

        private Entry* buckets;
        private int capacity;
        private int size;
        private const float loadFactor = 0.75f;
        private const int HashMask = 0x3FFFFFFF;
        private const int EmptyHashCode = unchecked((int)0x80000000);
        private const int TombstoneHashCode = unchecked((int)0x40000000);
        private const int SpecialBitsMask = unchecked((int)0xC0000000);

        public UnsafeDictionary(int initialCapacity)
        {
            Capacity = initialCapacity;
            size = 0;
        }

        public void Release()
        {
            Clear();
            Free(buckets);
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

                if (value != capacity)
                {
                    Entry* newBuckets = AllocT<Entry>(value);
                    MemsetT(newBuckets, Entry.Empty, capacity);

                    if (size > 0)
                    {
                        for (int i = 0; i < capacity; i++)
                        {
                            Entry* entry = &buckets[i];
                            if ((entry->HashCode & SpecialBitsMask) != 0) continue;
                            Entry* dest = FindEntry(newBuckets, capacity, entry->HashCode);
                            *dest = *entry;
                        }
                    }

                    Free(buckets);
                    buckets = newBuckets;
                    capacity = value;
                }
            }
        }

        private static Entry* FindEntry(Entry* entries, int capacity, int hashCode)
        {
            Entry* tombstone = null;
            int index = hashCode % capacity;
            while (true)
            {
                Entry* entry = &entries[index];
                if ((entry->HashCode & SpecialBitsMask) != 0)
                {
                    if (entry->HashCode == EmptyHashCode)
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
                else if (entry->HashCode == hashCode)
                {
                    // We found the key.
                    return entry;
                }

                index = (index + 1) % capacity;
            }
        }

        public void Grow(int capacity)
        {
            Capacity = capacity * 2;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity * loadFactor > this.capacity)
            {
                Grow(capacity);
            }
        }

        public void Add(TKey key, TValue value)
        {
            EnsureCapacity(size + 1);

            int hashCode = key.GetHashCode() & HashMask;

            Entry* entry = FindEntry(buckets, capacity, hashCode);

            bool isNewKey = entry->HashCode == EmptyHashCode;

            if (!isNewKey)
            {
                throw new ArgumentException("Key already exists in the dictionary");
            }

            entry->HashCode = hashCode;
            entry->Key = key;
            entry->Value = value;

            size++;
        }

        public void Set(TKey key, TValue value)
        {
            EnsureCapacity(size + 1);

            int hashCode = key.GetHashCode() & HashMask;

            Entry* entry = FindEntry(buckets, capacity, hashCode);

            bool isNewKey = entry->HashCode == EmptyHashCode;

            if (isNewKey)
            {
                size++;
            }

            entry->HashCode = hashCode;
            entry->Key = key;
            entry->Value = value;
        }

        public readonly bool ContainsKey(TKey key)
        {
            int hashCode = key.GetHashCode() & HashMask;
            Entry* entry = FindEntry(buckets, capacity, hashCode);
            return entry->HashCode != EmptyHashCode;
        }

        public bool Remove(TKey key)
        {
            if (size == 0)
            {
                return false;
            }

            int hashCode = key.GetHashCode() & HashMask;
            Entry* entry = FindEntry(buckets, capacity, hashCode);

            if (entry->HashCode == EmptyHashCode)
            {
                return false;
            }

            *entry = Entry.Tombstone;
            size--;

            return false;
        }

        public readonly bool TryGetValue(TKey key, out TValue value)
        {
            if (size == 0)
            {
                value = default;
                return false;
            }

            int hashCode = key.GetHashCode() & HashMask;
            Entry* entry = FindEntry(buckets, capacity, hashCode);

            if (entry->HashCode == EmptyHashCode)
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
                    if (current->HashCode == EmptyHashCode)
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
                    if (current->HashCode == EmptyHashCode)
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
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(array.Length - arrayIndex, size);

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
                if (itemIndex >= dictionary.size)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
                    if ((entry->HashCode & SpecialBitsMask) != 0)
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
                if (itemIndex >= dictionary.size)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
                    if ((entry->HashCode & SpecialBitsMask) != 0)
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
                if (itemIndex >= dictionary.size)
                {
                    return false;
                }

                for (int i = index; i < dictionary.capacity; i++)
                {
                    Entry* entry = &dictionary.buckets[i];
                    if ((entry->HashCode & SpecialBitsMask) != 0)
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
    }
}