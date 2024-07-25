namespace HexaEngine.Core
{
    using System.Collections.Concurrent;

    /// <summary>
    /// A thread-safe object pool for <see cref="List{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The type of elements in the lists.</typeparam>
    public class ListPool<T>
    {
        private readonly ConcurrentBag<List<T>> pool = new();

        /// <summary>
        /// Gets a shared instance of the <see cref="ListPool{T}"/> for convenient use.
        /// </summary>
        public static ListPool<T> Shared { get; } = new();

        /// <summary>
        /// Rents a <see cref="List{T}"/> instance from the pool. If the pool is empty, a new instance is created.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> instance from the pool or a new instance if the pool is empty.</returns>
        public List<T> Rent()
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
        /// Returns a rented <see cref="List{T}"/> instance to the pool after clearing its contents.
        /// </summary>
        /// <param name="list">The <see cref="List{T}"/> instance to return to the pool.</param>
        public void Return(List<T> list)
        {
            list.Clear();
            pool.Add(list);
        }

        /// <summary>
        /// Clears the pool, removing all <see cref="List{T}"/> instances from it.
        /// </summary>
        public void Clear()
        {
            pool.Clear();
        }
    }
}