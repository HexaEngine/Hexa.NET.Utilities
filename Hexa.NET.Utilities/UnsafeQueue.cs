namespace Hexa.NET.Utilities
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Represents an unsafe queue of elements of type T.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public unsafe struct UnsafeQueue<T> : IFreeable where T : unmanaged
    {
        private const int DefaultCapacity = 4;

        private T* items;
        private nint front;
        private nint rear;
        private nint size;
        private nint capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeQueue{T}"/> struct with the default capacity.
        /// </summary>
        public UnsafeQueue()
        {
            EnsureCapacity(DefaultCapacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsafeQueue{T}"/> struct with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the queue.</param>
        public UnsafeQueue(int capacity)
        {
            EnsureCapacity(capacity);
        }

        /// <summary>
        /// Gets the number of elements in the queue.
        /// </summary>
        public readonly nint Size => size;

        /// <summary>
        /// Gets or sets the capacity of the queue.
        /// </summary>
        public nint Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (value == capacity)
                {
                    return;
                }

                if (items == null)
                {
                    items = AllocT<T>(value);
                    return;
                }

                var tmp = AllocT<T>(value);

                if (size > 0)
                {
                    if (front <= rear)
                    {
                        MemcpyT(items + front, tmp, value, size);
                    }
                    else
                    {
                        MemcpyT(items + front, tmp, value, capacity - front);
                        MemcpyT(items, tmp + (capacity - front), value, rear);
                    }
                }

                Free(items);
                items = tmp;
                front = 0;
                rear = size - 1;
                capacity = value;
                size = capacity < size ? capacity : size;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => items[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => items[index] = value;
        }

        /// <summary>
        /// Gets the pointer to the items of the queue.
        /// </summary>
        public readonly T* Items => items;

        /// <summary>
        /// Gets the pointer to the front element of the queue.
        /// </summary>
        public readonly T* Front => items + front;

        /// <summary>
        /// Gets the pointer to the rear element of the queue.
        /// </summary>
        public readonly T* Rear => items + rear;

        /// <summary>
        /// Initializes the queue with the default capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init()
        {
            Grow(DefaultCapacity);
        }

        /// <summary>
        /// Initializes the queue with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the queue.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(int capacity)
        {
            Grow(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(nint capacity)
        {
            nint newcapacity = size == 0 ? DefaultCapacity : 2 * size;

            if (newcapacity < capacity)
            {
                newcapacity = capacity;
            }

            Capacity = newcapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Shrink()
        {
            if (capacity > DefaultCapacity && capacity > size * 2)
            {
                Capacity = size;
            }
        }

        /// <summary>
        /// Ensures that the queue has the specified capacity.
        /// </summary>
        /// <param name="capacity">The desired capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(nint capacity)
        {
            if (this.capacity < capacity)
            {
                Grow(capacity);
            }
        }

        /// <summary>
        /// Adds an item to the end of the queue.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            EnsureCapacity(size + 1);
            rear = (rear + 1) % capacity;
            items[rear] = item;
            size++;
        }

        /// <summary>
        /// Removes and returns the item at the front of the queue.
        /// </summary>
        /// <returns>The item that was dequeued.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (size == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            T item = items[front];
            front = (front + 1) % capacity;
            size--;
            Shrink();

            return item;
        }

        /// <summary>
        /// Attempts to remove and return the item at the front of the queue.
        /// </summary>
        /// <param name="item">When this method returns, contains the dequeued item, if the operation was successful.</param>
        /// <returns><c>true</c> if an item was successfully dequeued; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            if (size == 0)
            {
                item = default;
                return false;
            }

            item = items[front];
            front = (front + 1) % capacity;
            size--;
            Shrink();

            return true;
        }

        /// <summary>
        /// Copies the elements of the queue to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The starting index in the destination array.</param>
        /// <param name="arraySize">The size of the destination array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(T* array, int arrayIndex, int arraySize)
        {
            MemcpyT(items, &array[arrayIndex], arraySize - arrayIndex, size);
        }

        /// <summary>
        /// Copies a range of elements from the queue to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The starting index in the destination array.</param>
        /// <param name="arraySize">The size of the destination array.</param>
        /// <param name="offset">The starting index in the queue.</param>
        /// <param name="count">The number of elements to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T* array, int arrayIndex, int arraySize, int offset, int count)
        {
            MemcpyT(&items[offset], &array[arrayIndex], arraySize - arrayIndex, count - offset);
        }

        /// <summary>
        /// Clears the queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ZeroMemoryT(items, capacity);
            size = 0;
            front = 0;
            rear = 0;
        }

        /// <summary>
        /// Determines whether the queue contains the specified item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            for (nint i = 0; i < size; i++)
            {
                var current = items[(front + i) % capacity];

                if (current.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Releases the memory associated with the queue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            if (items != null)
            {
                Free(items);
                this = default;
            }
        }
    }
}