namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;

    public unsafe partial class Utils
    {
        public static int StrCmp(char* a, char* b)
        {
            if (a == null)
            {
                if (b != null)
                {
                    return -1;
                }

                return 0;
            }

            if (b == null)
            {
                return 1;
            }

            while (*a != 0 && *b != 0)
            {
                if (*a != *b)
                {
                    return *a - *b;
                }

                a++;
                b++;
            }

            if (*a == 0 && *b == 0)
            {
                return 0;
            }

            if (*a != 0)
            {
                return 1;
            }

            return -1;
        }

        /// <summary>
        /// Returns the size of a null-terminated string in bytes.
        /// </summary>
        /// <param name="str">Pointer to the null-terminated string.</param>
        /// <returns>The number of bytes in the null-terminated string, or 0 if the pointer is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrLen(char* str)
        {
            if (str == null)
            {
                return 0; // Return 0 for null pointer
            }

            int len = 0;
            while (*str != 0)
            {
                str++;
                len++;
            }

            return len;
        }

        /// <summary>
        /// Returns the size of a null-terminated string in bytes.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The number of bytes in the null-terminated string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrLen(ReadOnlySpan<char> str)
        {
            fixed (char* ptr = str)
                return StrLen(ptr);
        }
    }
}