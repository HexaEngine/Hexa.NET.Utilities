namespace HexaEngine.Core
{
    using System.Collections.Concurrent;

    /// <summary>
    /// A thread-safe object pool for <see cref="{T}"/> instances.
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
        /// Rents a <see cref="{T}"/> instance from the pool. If the pool is empty, a new instance is created.
        /// </summary>
        /// <returns>A <see cref="{T}"/> instance from the pool or a new instance if the pool is empty.</returns>
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
        /// Returns a rented <see cref="{T}"/> instance to the pool after clearing its contents.
        /// </summary>
        /// <param name="obj">The <see cref="{T}"/> instance to return to the pool.</param>
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
        /// Clears the pool, removing all <see cref="{T}"/> instances from it.
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