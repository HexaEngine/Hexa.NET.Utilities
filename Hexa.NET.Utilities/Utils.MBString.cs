namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;

    // Implemented:
    // MbStrLen

    public static unsafe partial class Utils
    {
        /// <summary>
        /// Returns the number of characters in a UTF-8 encoded multibyte string.
        /// </summary>
        /// <param name="ptr">Pointer to the UTF-8 encoded multibyte string.</param>
        /// <returns>The number of characters in the UTF-8 encoded multibyte string, or -1 if an invalid UTF-8 encoding is detected.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MbStrLen(byte* ptr)
        {
            if (ptr == null)
            {
                return 0; // Return 0 for null pointer
            }

            int count = 0;

            while (*ptr != 0)
            {
                byte b = *ptr;

                if ((b & 0x80) == 0)
                {
                    // 1-byte character (ASCII)
                    ptr += 1;
                }
                else if ((b & 0xE0) == 0xC0)
                {
                    // 2-byte character
                    if ((ptr[1] & 0xC0) != 0x80) // Check for valid continuation byte
                    {
                        return -1;
                    }
                    ptr += 2;
                }
                else if ((b & 0xF0) == 0xE0)
                {
                    // 3-byte character
                    if ((ptr[1] & 0xC0) != 0x80 || (ptr[2] & 0xC0) != 0x80) // Check for valid continuation bytes
                    {
                        return -1;
                    }
                    ptr += 3;
                }
                else if ((b & 0xF8) == 0xF0)
                {
                    // 4-byte character
                    if ((ptr[1] & 0xC0) != 0x80 || (ptr[2] & 0xC0) != 0x80 || (ptr[3] & 0xC0) != 0x80) // Check for valid continuation bytes
                    {
                        return -1;
                    }
                    ptr += 4;
                }
                else
                {
                    return -1;
                }
                count++;
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint ReadNextChar(byte** pPtr)
        {
            uint codePoint;

            byte* ptr = *pPtr;
            byte b = *ptr;

            if ((b & 0x80) == 0)
            {
                codePoint = b;
                ptr += 1;
            }
            else if ((b & 0xE0) == 0xC0)
            {
                codePoint = ((uint)(b & 0x1F) << 6) | (uint)(ptr[1] & 0x3F);
                ptr += 2;
            }
            else if ((b & 0xF0) == 0xE0)
            {
                codePoint = ((uint)(b & 0x0F) << 12) | ((uint)(ptr[1] & 0x3F) << 6) | (uint)(ptr[2] & 0x3F);
                ptr += 3;
            }
            else if ((b & 0xF8) == 0xF0)
            {
                codePoint = ((uint)(b & 0x07) << 18) | ((uint)(ptr[1] & 0x3F) << 12) | ((uint)(ptr[2] & 0x3F) << 6) | (uint)(ptr[3] & 0x3F);
                ptr += 4;
            }
            else
            {
                return unchecked((uint)-1);
            }

            *pPtr = ptr;

            return codePoint;
        }
    }
}
