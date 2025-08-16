namespace Hexa.NET.Utilities
{
    using System;

    public static unsafe partial class Utils
    {
        public static void QSort<T>(T* data, int length, Comparison<T> comparer) where T : unmanaged
        {
            if (length <= 1)
                return;

            QSortInternal(data, data + length - 1, comparer);
        }

        private static void QSortInternal<T>(T* left, T* right, Comparison<T> comparer) where T : unmanaged
        {
            if (left >= right)
                return;

            T* pivot = Partition(left, right, comparer);
            QSortInternal(left, pivot - 1, comparer);  // Sort left partition
            QSortInternal(pivot + 1, right, comparer); // Sort right partition
        }

        private static T* Partition<T>(T* left, T* right, Comparison<T> comparer) where T : unmanaged
        {
            T pivotValue = *right; // Choose the last element as pivot
            T* i = left - 1;

            for (T* j = left; j < right; j++)
            {
                if (comparer(*j, pivotValue) <= 0)
                {
                    i++;
                    Swap(i, j);
                }
            }

            Swap(i + 1, right);
            return i + 1; // Return pivot position
        }
    }
}