namespace Hexa.NET.Utilities
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;

    public unsafe struct UnsafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IEquatable<UnsafeDictionary<TKey, TValue>>, IFreeable where TKey : unmanaged where TValue : unmanaged
    {
        private Entry* buckets;
        private int capacity;
        private int size;
        private void* comparer; // use void* to avoid problems with Visual Studio; Visual Studio has a problem with generic delegate pointers.

        private const float loadFactor = 0.75f;

        internal enum EntryFlags : byte
        {
            Empty = 0,
            Tombstone = 1,
            Filled = 2
        }

        internal struct Entry
        {
            public uint HashCode;
            public TKey Key;
            public TValue Value;
            public EntryFlags Flags;

            public Entry(uint hashCode, TKey key, TValue value, EntryFlags flags)
            {
                HashCode = hashCode;
                Key = key;
                Value = value;
                Flags = flags;
            }

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

        private readonly static Entry Empty = new(0, default, default, EntryFlags.Empty);

        private readonly static Entry Tombstone = new(0, default, default, EntryFlags.Tombstone);

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
                ZeroMemoryT(newBuckets, value);

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

        private readonly Entry* FindEntry(Entry* entries, int capacity, TKey key, uint hashCode)
        {
            Entry* tombstone = null;
            uint index = (uint)(hashCode % capacity);
            bool exit = false;
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

                index++; // this is faster than %.
                if (index == capacity)
                {
                    if (exit)
                    {
                        break;
                    }
                    index = 0;
                    exit = true;
                }
            }

            // this should never happen only if someone forgot to call EnsureCapacity.
            throw new InvalidOperationException("Infinite loop detected in FindEntry.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool Compare(TKey x, TKey y)
        {
            if (comparer == null)
            {
                return EqualityComparer<TKey>.Default.Equals(x, y);
            }
            return ((delegate*<TKey, TKey, bool>)comparer)(x, y);
        }

        public void Grow(int capacity)
        {
            Capacity = HashHelpers.ExpandPrime(capacity);
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
            Capacity = (int)(size * (1 / loadFactor));
        }

        public void Add(TKey key, TValue value)
        {
            EnsureCapacity(size + 1);

            uint hashCode = (uint)key.GetHashCode();

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

            uint hashCode = (uint)key.GetHashCode();

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
            uint hashCode = (uint)key.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, key, hashCode);
            return entry->HashCode == hashCode;
        }

        public bool Remove(TKey key)
        {
            if (size == 0)
            {
                return false;
            }

            uint hashCode = (uint)key.GetHashCode();
            Entry* entry = FindEntry(buckets, capacity, key, hashCode);

            if (!entry->IsFilled)
            {
                return false;
            }

            *entry = Tombstone;
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

            uint hashCode = (uint)key.GetHashCode();
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
            ZeroMemoryT(buckets, capacity);
            size = 0;
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        ///
        /// Similar to the `Values` property, this collection reflects the state of the dictionary
        /// at the time it is accessed. Subsequent modifications to the dictionary or copies made
        /// of the dictionary will not affect this collection, and no modification exceptions will
        /// be thrown during enumeration.
        ///
        /// This behavior results from `UnsafeDictionary` being a value type (`struct`), where each
        /// copy of the dictionary is independent and version tracking is not feasible.
        /// </summary>
        readonly ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get => new KeyCollection(this);
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        ///
        /// Note: Since `UnsafeDictionary` is implemented as a `struct`, this collection is
        /// tied to the state of the dictionary at the time it is accessed. If the dictionary
        /// is copied or modified after this collection is accessed, the `ValuesCollection`
        /// will not reflect those changes, and no exception will be thrown.
        ///
        /// Because the `UnsafeDictionary` is a value type, each copy of the dictionary, including
        /// those made implicitly when passing the dictionary to methods or properties, is independent.
        /// As such, the typical version tracking used in reference types (`class`) is not applicable.
        /// </summary>
        readonly ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get => new ValueCollection(this);
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
                if (itemIndex >= dictionary.size - 1)
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
                if (itemIndex >= dictionary.size - 1)
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
                if (itemIndex >= dictionary.size - 1)
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

        public readonly struct KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly UnsafeDictionary<TKey, TValue> dictionary;

            public KeyCollection(UnsafeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public int Count => dictionary.size;

            public bool IsReadOnly => true;

            public bool IsSynchronized => false;

            public object SyncRoot => null!;

            public void Add(TKey item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Remove(TKey item) => throw new NotSupportedException();

            public bool Contains(TKey item)
            {
                return dictionary.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                foreach (var pair in dictionary)
                {
                    array[arrayIndex++] = pair.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new KeyEnumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void CopyTo(Array array, int index)
            {
                foreach (var pair in dictionary)
                {
                    array.SetValue(pair.Key, index++);
                }
            }
        }

        public readonly struct ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly UnsafeDictionary<TKey, TValue> dictionary;

            public ValueCollection(UnsafeDictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
            }

            public int Count => dictionary.size;

            public bool IsReadOnly => true;

            public bool IsSynchronized => false;

            public object SyncRoot => null!;

            public void Add(TValue item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Remove(TValue item) => throw new NotSupportedException();

            public bool Contains(TValue item)
            {
                foreach (var pair in dictionary)
                {
                    if (EqualityComparer<TValue>.Default.Equals(pair.Value, item))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                foreach (var pair in dictionary)
                {
                    array[arrayIndex++] = pair.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new ValueEnumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void CopyTo(Array array, int index)
            {
                foreach (var pair in dictionary)
                {
                    array.SetValue(pair.Value, index++);
                }
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

        public override readonly string ToString()
        {
            return $"Count = {size}";
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