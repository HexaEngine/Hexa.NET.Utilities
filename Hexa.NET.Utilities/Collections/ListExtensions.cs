﻿#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities.Collections
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides extension methods for <see cref="List{T}"/> and accessing the internal array.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Allows to access the internal array of a <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list for which to access the internal array.</param>
        /// <returns>The internal array of the list.</returns>
        public static T[] GetInternalArray<T>(this List<T> list)
        {
            return ArrayAccessor<T>.Getter(list);
        }

        /// <summary>
        /// Mutates the item at the specified index in the list using the provided function.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to mutate.</param>
        /// <param name="index">The index of the item to mutate.</param>
        /// <param name="func">The function to apply to the item at the specified index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MutateItem<T>(this IList<T> list, int index, Func<T, T> func)
        {
            var item = list[index];
            item = func(item);
            list[index] = item;
        }
    }
}
#endif