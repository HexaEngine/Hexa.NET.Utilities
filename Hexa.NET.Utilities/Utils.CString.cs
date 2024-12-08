namespace Hexa.NET.Utilities
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;

    // Implemented:
    // StrLen
    // StrCpy
    // StrNCpy
    // StrCmp
    // StrNCmp
    // StrCaseCmp
    // StrCaseNCmp
    // ToLower
    // ToUpper
    // StrCat
    // StrNCat
    // StrChr
    // StrRChr
    // StrStr
    // StrSpn

    // TODO:
    // StrColl // We might have to create a helper function for this in c.
    // StrXFrm // We might have to create a helper function for this in c.
    // MemChr
    // StrCSpn
    // StrPBrk
    // StrTok

    public static unsafe partial class Utils
    {
        /// <summary>
        /// Returns the size of a null-terminated string in bytes.
        /// </summary>
        /// <param name="str">Pointer to the null-terminated string.</param>
        /// <returns>The number of bytes in the null-terminated string, or 0 if the pointer is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrLen(byte* str)
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
        public static int StrLen(ReadOnlySpan<byte> str)
        {
            fixed (byte* ptr = str)
                return StrLen(ptr);
        }

        /// <summary>
        /// Copies one string to another.
        /// </summary>
        /// <param name="dest">The pointer to the destination string.</param>
        /// <param name="src">The pointer to the source string.</param>
        /// <returns>Returns the destination pointer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrCpy(byte* dest, byte* src)
        {
            if (src == null || dest == null)
                return dest;
            byte* tmp = dest;
            while (*src != 0)
            {
                *tmp = *src;
                tmp++;
                src++;
            }
            *tmp = 0; // Null-terminate
            return dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* StrNCpy(byte* dest, byte* src, nint size)
        {
            if (src == null || dest == null)
                return dest;
            byte* tmp = dest;
            while (size > 0 && *src != 0)
            {
                *tmp = *src;
                tmp++;
                src++;
                size--;
            }
            *tmp = 0; // Null-terminate
            return dest;
        }

        /// <summary>
        /// Compares two null-terminated byte strings.
        /// </summary>
        /// <param name="a">The pointer to the first byte string.</param>
        /// <param name="b">The pointer to the second byte string.</param>
        /// <returns>
        /// An integer indicating the result of the comparison:
        /// <list type="bullet">
        /// <item>
        /// <description>0 if the strings are equal.</description>
        /// </item>
        /// <item>
        /// <description>A negative value if the first string is less than the second string.</description>
        /// </item>
        /// <item>
        /// <description>A positive value if the first string is greater than the second string.</description>
        /// </item>
        /// </list>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int StrCmp(byte* a, byte* b)
        {
            if (a == null)
            {
                return b == null ? 0 : -1;
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
            return *a == 0 ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int StrNCmp(byte* a, byte* b, nint count)
        {
            if (a == null)
            {
                return b == null ? 0 : -1;
            }
            if (b == null)
            {
                return 1;
            }

            while (count > 0 && *a != 0 && *b != 0)
            {
                if (*a != *b)
                {
                    return *a - *b;
                }
                a++;
                b++;
                count--;
            }

            if (*a == 0 && *b == 0)
            {
                return 0;
            }
            return *a == 0 ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrCaseCmp(byte* a, byte* b)
        {
            if (a == null)
            {
                return b == null ? 0 : -1;
            }
            if (b == null)
            {
                return 1;
            }

            while (*a != 0 && *b != 0)
            {
                var aa = ToLower(*a);
                var bb = ToLower(*b);
                if (aa != bb)
                {
                    return aa - bb;
                }
                a++;
                b++;
            }

            if (*a == 0 && *b == 0)
            {
                return 0;
            }
            return *a == 0 ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrNCaseCmp(byte* a, byte* b, nint count)
        {
            if (a == null)
            {
                return b == null ? 0 : -1;
            }
            if (b == null)
            {
                return 1;
            }

            while (count > 0 && *a != 0 && *b != 0)
            {
                var aa = ToLower(*a);
                var bb = ToLower(*b);
                if (aa != bb)
                {
                    return aa - bb;
                }
                a++;
                b++;
                count--;
            }

            if (*a == 0 && *b == 0)
            {
                return 0;
            }
            return *a == 0 ? -1 : 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToLower(byte c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return (byte)(c + ('a' - 'A'));
            }
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToUpper(byte c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return (byte)(c + ('A' - 'a'));
            }
            return c;
        }

        /// <summary>
        /// Concatenates the source string to the destination string.
        /// </summary>
        /// <param name="dest">Pointer to the destination string buffer.</param>
        /// <param name="src">Pointer to the source string to be concatenated.</param>
        /// <returns>Pointer to the destination string buffer after concatenation.</returns>
        /// <remarks>
        /// The destination string must have enough space to hold the resulting concatenated string.
        /// If either <paramref name="dest"/> or <paramref name="src"/> is <c>null</c>, <paramref name="dest"/> is returned unchanged.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrCat(byte* dest, byte* src)
        {
            if (dest == null || src == null)
                return dest;

            int destLen = StrLen(dest);
            byte* tmp = dest + destLen;
            while (*src != 0)
            {
                *tmp = *src;
                tmp++;
                src++;
            }
            *tmp = 0; // Null-terminate the resulting string

            return dest;
        }

        /// <summary>
        /// Concatenates a specified number of characters from one string to another.
        /// </summary>
        /// <param name="dest">The pointer to the destination string.</param>
        /// <param name="src">The pointer to the source string.</param>
        /// <param name="count">The number of characters to concatenate.</param>
        /// <returns>Returns the destination pointer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrNCat(byte* dest, byte* src, nint count)
        {
            if (dest == null || src == null)
                return dest;

            int destLen = StrLen(dest);
            byte* tmp = dest + destLen;
            while (count > 0 && *src != 0)
            {
                *tmp = *src;
                tmp++;
                src++;
                count--;
            }
            *tmp = 0; // Null-terminate
            return dest;
        }

        /// <summary>
        /// Finds the first occurrence of a specified character in a string.
        /// </summary>
        /// <param name="str">The pointer to the string.</param>
        /// <param name="c">The character to find.</param>
        /// <returns>Returns a pointer to the first occurrence of the character, or null if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrChr(byte* str, byte c)
        {
            if (str == null)
                return null;

            while (*str != 0)
            {
                if (*str == c)
                    return str;
                str++;
            }
            return null;
        }

        /// <summary>
        /// Finds the last occurrence of a specified character in a string.
        /// </summary>
        /// <param name="str">The pointer to the string.</param>
        /// <param name="c">The character to find.</param>
        /// <returns>Returns a pointer to the last occurrence of the character, or null if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrRChr(byte* str, byte c)
        {
            if (str == null)
                return null;

            byte* last = null;
            while (*str != 0)
            {
                if (*str == c)
                    last = str;
                str++;
            }
            return last;
        }

        /// <summary>
        /// Searches for the first occurrence of a substring within a string.
        /// </summary>
        /// <param name="str">Pointer to the string to be searched.</param>
        /// <param name="subStr">Pointer to the substring to search for.</param>
        /// <returns>Pointer to the first occurrence of <paramref name="subStr"/> within <paramref name="str"/>. If <paramref name="subStr"/> is not found, or if either <paramref name="str"/> or <paramref name="subStr"/> is <c>null</c>, <c>null</c> is returned.</returns>
        /// <remarks>
        /// This function searches for the first occurrence of the substring <paramref name="subStr"/> within the string <paramref name="str"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* StrStr(byte* str, byte* subStr)
        {
            // assuming the length is known.
            // At best this is O(m)
            // At avg this is O(n)
            // At worst this is O(n)

            if (str == null || subStr == null)
                return null;

            int subStrLen = StrLen(subStr);
            byte* tmp = str;
            int cmp = 0;
            while (*tmp != 0)
            {
                byte b = subStr[cmp];
                if (b == *tmp)
                {
                    cmp++;
                    if (cmp == subStrLen)
                    {
                        return tmp - cmp + 1;
                    }
                }
                else
                {
                    cmp = 0;
                }
                tmp++;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrSpn(byte* str, byte* accept)
        {
            int byteCount = byte.MaxValue >> 3; // 256 / 8
            byte* acceptSet = stackalloc byte[byteCount];
            BitHelper bitHelper = new(acceptSet, byteCount);
            byte* acceptPtr = accept;
            while (*acceptPtr != 0)
            {
                bitHelper.MarkBit(*acceptPtr);
                acceptPtr++;
            }

            int length = 0;
            while (*str != 0)
            {
                if (bitHelper.IsMarked(*str))
                {
                    length++;
                    str++;
                }
                else
                {
                    break;
                }
            }
            return length;
        }

        public static int GetByteCountUTF8(string str)
        {
            return Encoding.UTF8.GetByteCount(str);
        }

        public static void EncodeStringUTF8(string str, byte* dst, int length)
        {
            fixed (char* src = str)
            {
                Encoding.UTF8.GetBytes(src, str.Length, dst, length);
            }
        }
    }
}