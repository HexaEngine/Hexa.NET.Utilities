namespace Hexa.NET.Utilities
{
    using System.Collections.Concurrent;

    /// <summary>
    /// A thread-safe object pool for <typeparamref name="T"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of elements in the lists.</typeparam>
    public class ObjectPool<T> where T : new()
    {
        private readonly ConcurrentBag<T> pool = new();
        private int maxItemsThreshold = 128;

        /// <summary>
        /// Gets a shared instance of the <see cref="ObjectPool{T}"/> for convenient use.
        /// </summary>
        public static ObjectPool<T> Shared { get; } = new();

        /// <summary>
        /// Gets or sets the maximum number of items that can be stored in the pool.
        /// </summary>
        public int MaxItemsThreshold { get => maxItemsThreshold; set => maxItemsThreshold = value; }

        /// <summary>
        /// Rents a <typeparamref name="T"/> instance from the pool. If the pool is empty, a new instance is created.
        /// </summary>
        /// <returns>A <typeparamref name="T"/> instance from the pool or a new instance if the pool is empty.</returns>
        public T Rent()
        {
            if (pool.IsEmpty)
            {
                return new();
            }
            else
            {
                if (pool.TryTake(out var list))
                {
                    return list;
                }
                return new();
            }
        }

        /// <summary>
        /// Returns a rented <typeparamref name="T"/> instance to the pool after clearing its contents.
        /// </summary>
        /// <param name="obj">The <typeparamref name="T"/> instance to return to the pool.</param>
        public void Return(T obj)
        {
            if (pool.Count > maxItemsThreshold)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                return;
            }
            pool.Add(obj);
        }

        /// <summary>
        /// Clears the pool, removing all <typeparamref name="T"/> instances from it.
        /// </summary>
        public void Clear()
        {
            while (pool.TryTake(out var result))
            {
                if (result is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}