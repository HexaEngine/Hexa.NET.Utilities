﻿namespace Hexa.NET.Utilities
{
    using System;
    using System.Collections;
    using System.Text;

    public static class StringSpanComparer
    {
        public static int Compare(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB, StringComparison comparison)
        {
            return comparison switch
            {
                StringComparison.Ordinal => CompareOrdinal(strA, strB),
                StringComparison.OrdinalIgnoreCase => CompareOrdinalIgnoreCase(strA, strB),
                _ => throw new ArgumentException($"StringComparison mode {comparison} is not supported."),
            };
        }

        public static int Compare(ReadOnlySpan<byte> strA, ReadOnlySpan<char> strB, StringComparison comparison)
        {
            return comparison switch
            {
                StringComparison.Ordinal => CompareOrdinal(strA, strB),
                StringComparison.OrdinalIgnoreCase => CompareOrdinalIgnoreCase(strA, strB),
                _ => throw new ArgumentException($"StringComparison mode {comparison} is not supported."),
            };
        }

        public static int Compare(ReadOnlySpan<char> strA, ReadOnlySpan<byte> strB, StringComparison comparison)
        {
            return comparison switch
            {
                StringComparison.Ordinal => CompareOrdinal(strA, strB),
                StringComparison.OrdinalIgnoreCase => CompareOrdinalIgnoreCase(strA, strB),
                _ => throw new ArgumentException($"StringComparison mode {comparison} is not supported."),
            };
        }

        private static int CompareOrdinal(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        private static int CompareOrdinalIgnoreCase(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = char.ToUpperInvariant((char)strA[i]).CompareTo(char.ToUpperInvariant((char)strB[i]));
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        private static int CompareOrdinal(ReadOnlySpan<char> strA, ReadOnlySpan<byte> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        private static int CompareOrdinalIgnoreCase(ReadOnlySpan<char> strA, ReadOnlySpan<byte> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = char.ToUpperInvariant(strA[i]).CompareTo(char.ToUpperInvariant((char)strB[i]));
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        private static int CompareOrdinal(ReadOnlySpan<byte> strA, ReadOnlySpan<char> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        private static int CompareOrdinalIgnoreCase(ReadOnlySpan<byte> strA, ReadOnlySpan<char> strB)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = char.ToUpperInvariant((char)strA[i]).CompareTo(char.ToUpperInvariant(strB[i]));
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }
    }

    public readonly struct WCharToCharConverter : IConverter<char, byte>
    {
        public static readonly WCharToCharConverter Default = new();

        public readonly byte Convert(char value)
        {
            return (byte)value;
        }
    }

    public readonly struct CharToWCharConverter : IConverter<byte, char>
    {
        public static readonly CharToWCharConverter Default = new();

        public readonly char Convert(byte value)
        {
            return (char)value;
        }
    }

    /// <summary>
    /// Represents a C++-style std::string implemented in C#.
    /// </summary>
    public unsafe struct StdString : IEnumerable<byte>
    {
        private const int DefaultCapacity = 4;

        private byte* data;
        private int size;
        private int capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="StdString"/> struct with default capacity.
        /// </summary>
        public StdString()
        {
            data = AllocT<byte>(DefaultCapacity + 1);
            capacity = DefaultCapacity;
            ZeroMemoryT(data, capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StdString"/> struct with a specified capacity.
        /// </summary>
        public StdString(int capacity)
        {
            data = AllocT<byte>(capacity + 1);
            this.capacity = capacity + 1;
            ZeroMemoryT(data, capacity + 1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StdString"/> struct from a C# string.
        /// </summary>
        public StdString(string s)
        {
            var byteCount = Encoding.UTF8.GetByteCount(s);

            data = AllocT<byte>(byteCount + 1);
            ZeroMemoryT(data, byteCount + 1);
            capacity = size = byteCount;
            fixed (char* chars = s)
            {
                Encoding.UTF8.GetBytes(chars, s.Length, data, byteCount);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StdString"/> struct.
        /// </summary>
        public StdString(byte* data, int size, int capacity)
        {
            this.data = data;
            this.size = size;
            this.capacity = capacity;
        }

        /// <summary>
        /// Gets a pointer to the internal data.
        /// </summary>
        public readonly byte* Data => data;

        /// <summary>
        /// Gets the size (length) of the string.
        /// </summary>
        public readonly int Size => size;

        /// <summary>
        /// Gets a pointer to the first byte of the string.
        /// </summary>
        public readonly byte* Front => data;

        /// <summary>
        /// Gets a pointer to the last byte of the string.
        /// </summary>
        public readonly byte* Back => data + size - 1;

        /// <summary>
        /// Gets or sets the capacity of the string. Adjusting the capacity can change the memory allocated for the string.
        /// </summary>
        public int Capacity
        {
            readonly get => capacity;
            set
            {
                capacity = value;
                size = size > value ? value : size;
                data = ReAllocT(data, value + 1);
                for (int i = size; i < value + 1; i++)
                {
                    data[i] = (byte)'\0';
                }
            }
        }

        /// <summary>
        /// Gets or sets a byte at a specified index in the string.
        /// </summary>
        /// <param name="index">The index of the byte to get or set.</param>
        /// <returns>The byte at the specified index.</returns>
        public byte this[int index]
        {
            get => data[index];
            set => data[index] = value;
        }

        /// <summary>
        /// Gets a C-style null-terminated string (char*) from the data.
        /// </summary>
        /// <returns>A pointer to the null-terminated string.</returns>
        public readonly byte* CStr()
        {
            return data;
        }

        /// <summary>
        /// Retrieves the byte at the specified index, throwing exceptions for invalid index values.
        /// </summary>
        /// <param name="index">The index of the byte to retrieve.</param>
        /// <returns>The byte at the specified index.</returns>
        public byte At(int index)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, size);
#else
            if (index < 0 || index >= size)
            {
                throw new ArgumentOutOfRangeException();
            }
#endif
            return this[index];
        }

        /// <summary>
        /// Increases the capacity of the string to a specified value.
        /// </summary>
        /// <param name="capacity">The desired capacity.</param>
        public void Grow(int capacity)
        {
            if (this.capacity < capacity || data == null)
            {
                Capacity = capacity;
            }
        }

        /// <summary>
        /// Ensures that the capacity of the string is at least a specified value.
        /// </summary>
        /// <param name="capacity">The desired capacity.</param>
        public void EnsureCapacity(int capacity)
        {
            if (this.capacity < capacity || data == null)
            {
                Grow(capacity * 2);
            }
        }

        /// <summary>
        /// Reduces the capacity of the string to match the current size.
        /// </summary>
        public void ShrinkToFit()
        {
            Capacity = size;
        }

        /// <summary>
        /// Resizes the string to the specified size, padding with null bytes if necessary.
        /// </summary>
        /// <param name="size">The new size of the string.</param>
        public void Resize(int size)
        {
            EnsureCapacity(size);
            this.size = size;
        }

        /// <summary>
        /// Inserts a byte at the specified index in the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="index">The index where the byte is inserted.</param>
        /// <param name="item">The byte to insert.</param>
        public void Insert(int index, byte item)
        {
            EnsureCapacity(size + 1);
            MemcpyT(&data[index], &data[index + 1], size - index);
            data[index] = item;
            size++;
        }

        /// <summary>
        /// Inserts the contents of another <see cref="StdString"/> at the specified index in the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="index">The index where the insertion starts.</param>
        /// <param name="item">The <see cref="StdString"/> to insert.</param>
        public void InsertRange(int index, StdString item)
        {
            EnsureCapacity(size + item.size);
            MemcpyT(&data[index], &data[index + item.size], size - index);
            for (int i = 0; i < item.size; i++)
            {
                data[index + i] = item[i];
            }

            size += item.size;
        }

        public void InsertRange(int index, byte* str, int count)
        {
            EnsureCapacity(size + count);
            MemcpyT(&data[index], &data[index + count], size - index);
            MemcpyT(str, &data[index], count);
            size += count;
        }

        /// <summary>
        /// Inserts a string at the specified index in the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="index">The index where the insertion starts.</param>
        /// <param name="item">The string to insert.</param>
        public void InsertRange(int index, string item)
        {
            EnsureCapacity(size + item.Length);
            MemcpyT(&data[index], &data[index + item.Length], size - index);
            for (int i = 0; i < item.Length; i++)
            {
                data[index + i] = (byte)item[i];
            }

            size += item.Length;
        }

        /// <summary>
        /// Appends a byte to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The byte to append.</param>
        public void Append(byte c)
        {
            EnsureCapacity(size + 1);
            data[size] = c;
            size++;
        }

        /// <summary>
        /// Appends a UTF8 string to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The pointer to append.</param>
        /// <param name="length">The length of the pointer</param>
        public void Append(byte* c, int length)
        {
            EnsureCapacity(size + length);
            MemcpyT(c, data + size, length);
            size += length;
        }

        /// <summary>
        /// Appends a byte to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The byte to append.</param>
        public void Append(byte* c)
        {
            int len = StrLen(c);
            Append(c, len);
        }

        /// <summary>
        /// Appends the contents of another <see cref="StdString"/> to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The <see cref="StdString"/> to append.</param>
        public void Append(StdString c)
        {
            EnsureCapacity(size + c.size);
            MemcpyT(c.data, data + size, c.size);
            size += c.size;
        }

        /// <summary>
        /// Appends the contents of a byte Span to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The pointer to append.</param>
        public void Append(ReadOnlySpan<byte> c)
        {
            fixed (byte* pc = c)
            {
                Append(pc, c.Length);
            }
        }

        /// <summary>
        /// Appends the contents of a UTF16 string to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The pointer to append.</param>
        /// <param name="length">The length of the pointer</param>
        public void Append(char* c, int length)
        {
            int charCount = Encoding.UTF8.GetByteCount(c, length);
            EnsureCapacity(size + charCount);
            Encoding.UTF8.GetBytes(c, length, data + size, charCount);
            size += charCount;
        }

        /// <summary>
        /// Appends the contents of a UTF16 null-terminated string to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The pointer to append.</param>
        public void Append(char* c)
        {
            int length = StrLen(c);
            Append(c, length);
        }

        /// <summary>
        /// Appends the contents of another <see cref="StdWString"/> to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The <see cref="StdWString"/> to append.</param>
        public void Append(StdWString c)
        {
            Append(c.Data, c.Size);
        }

        /// <summary>
        /// Appends the contents of a char Span to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="c">The Span to append.</param>
        public void Append(ReadOnlySpan<char> c)
        {
            fixed (char* pc = c)
            {
                Append(pc, c.Length);
            }
        }

        /// <summary>
        /// Clears the contents of the current <see cref="StdString"/>.
        /// </summary>
        public void Clear()
        {
            ZeroMemoryT(data, size);
            size = 0;
        }

        /// <summary>
        /// Clears the contents of the current <see cref="StdString"/> without changing its size.
        /// </summary>
        public readonly void Erase()
        {
            ZeroMemoryT(data, size);
        }

        public void Erase(int start, int len)
        {
            if (start < 0 || start >= size || len <= 0 || start + len > size)
            {
                throw new ArgumentOutOfRangeException();
            }

            int newSize = size - len;
            if (newSize > 0)
            {
                MemcpyT(data + start + len, data + start, newSize - start, newSize - start);
            }

            size = newSize;
        }

        /// <summary>
        /// Compares the current <see cref="StdString"/> with a specified byte sequence.
        /// </summary>
        /// <param name="other">The byte sequence to compare against.</param>
        /// <returns><c>true</c> if the contents are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(ReadOnlySpan<byte> other)
        {
            if (size != other.Length)
            {
                return false;
            }

            for (int i = 0; i < size; i++)
            {
                if (data[i] != other[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Compares the current <see cref="StdString"/> with a specified character sequence.
        /// </summary>
        /// <param name="other">The character sequence to compare against.</param>
        /// <returns><c>true</c> if the contents are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(ReadOnlySpan<char> other)
        {
            if (size != other.Length)
            {
                return false;
            }

            for (int i = 0; i < size; i++)
            {
                if (data[i] != other[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static int Compare(StdString strA, ReadOnlySpan<byte> strB)
        {
            var size = Math.Min(strA.size, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = ((char)strA[i]).CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.size.CompareTo(strB.Length);
        }

        public static int Compare(StdString strA, ReadOnlySpan<char> strB)
        {
            var size = Math.Min(strA.size, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.size.CompareTo(strB.Length);
        }

        public static int Compare(StdString strA, StdString strB)
        {
            var size = Math.Min(strA.size, strB.size);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.size.CompareTo(strB.size);
        }

        public static int Compare(ReadOnlySpan<byte> strA, StdString strB)
        {
            var size = Math.Min(strA.Length, strB.size);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.size);
        }

        public static int Compare(ReadOnlySpan<char> strA, StdString strB)
        {
            var size = Math.Min(strA.Length, strB.size);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.size);
        }

        public static int Compare(ReadOnlySpan<byte> strA, ReadOnlySpan<byte> strB, StringComparison comparison)
        {
            var size = Math.Min(strA.Length, strB.Length);
            for (int i = 0; i < size; i++)
            {
                var cmp = strA[i].CompareTo(strB[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return strA.Length.CompareTo(strB.Length);
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> starts with a specified byte sequence.
        /// </summary>
        /// <param name="str">The byte sequence to check for at the beginning.</param>
        /// <returns><c>true</c> if the string starts with the specified sequence; otherwise, <c>false</c>.</returns>
        public bool StartsWith(ReadOnlySpan<byte> str)
        {
            if (size < str.Length)
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (data[i] != str[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> ends with a specified byte sequence.
        /// </summary>
        /// <param name="str">The byte sequence to check for at the end.</param>
        /// <returns><c>true</c> if the string ends with the specified sequence; otherwise, <c>false</c>.</returns>
        public bool EndsWith(ReadOnlySpan<byte> str)
        {
            if (size < str.Length)
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (data[size - 1 - i] != str[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> starts with a specified character sequence.
        /// </summary>
        /// <param name="str">The character sequence to check for at the beginning.</param>
        /// <returns><c>true</c> if the string starts with the specified sequence; otherwise, <c>false</c>.</returns>
        public bool StartsWith(ReadOnlySpan<char> str)
        {
            if (size < str.Length)
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (data[i] != str[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> ends with a specified character sequence.
        /// </summary>
        /// <param name="str">The character sequence to check for at the end.</param>
        /// <returns><c>true</c> if the string ends with the specified sequence; otherwise, <c>false</c>.</returns>
        public bool EndsWith(ReadOnlySpan<char> str)
        {
            if (size < str.Length)
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (data[size - 1 - i] != str[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> contains a specified byte sequence.
        /// </summary>
        /// <param name="str">The byte sequence to check for.</param>
        /// <returns><c>true</c> if the string contains the specified sequence; otherwise, <c>false</c>.</returns>
        public readonly bool Contains(ReadOnlySpan<byte> str)
        {
            fixed (byte* pStr = str)
            {
                return Utils.Contains(data, size, pStr, str.Length);
            }
        }

        /// <summary>
        /// Checks if the current <see cref="StdString"/> contains a specified character sequence.
        /// </summary>
        /// <param name="str">The character sequence to check for.</param>
        /// <returns><c>true</c> if the string contains the specified sequence; otherwise, <c>false</c>.</returns>
        public readonly bool Contains(ReadOnlySpan<char> str)
        {
            fixed (char* pStr = str)
            {
                return Utils.Contains(data, size, pStr, str.Length, x => (byte)x);
            }
        }

        /// <summary>
        /// Replaces all occurrences of a target byte with a replacement byte in the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="target">The byte to find and replace.</param>
        /// <param name="replacement">The byte to replace the target with.</param>
        public readonly void Replace(byte target, byte replacement)
        {
            for (int i = 0; i < size; i++)
            {
                if (data[i] == target)
                {
                    data[i] = replacement;
                }
            }
        }

        /// <summary>
        /// Replaces all occurrences of a specific byte with another byte.
        /// </summary>
        /// <param name="target">The byte to be replaced.</param>
        /// <param name="replacement">The byte to replace with.</param>
        public void Replace(ReadOnlySpan<byte> target, ReadOnlySpan<byte> replacement)
        {
            if (size < target.Length)
            {
                return;
            }

            int cmp = 0;

            for (int i = 0; i < size; i++)
            {
                if (data[i] == target[cmp])
                {
                    cmp++;

                    if (cmp == target.Length)
                    {
                        var idx = i - cmp + 1;
                        var newSize = size - cmp + replacement.Length;

                        Grow(newSize);

                        if (i + 1 != size)
                        {
                            // copy data forward/backwards to ensure no data is lost or is additionally there where no data should be.
                            int delta = replacement.Length - target.Length;
                            MemcpyT(data + idx + target.Length, data + idx + replacement.Length, size - delta);
                        }

                        for (int j = 0; j < replacement.Length; j++)
                        {
                            data[idx + j] = replacement[j];
                        }

                        size = newSize;
                        i = idx + replacement.Length;
                        cmp = 0;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }
        }

        /// <summary>
        /// Replaces all occurrences of a specific sequence of bytes with another sequence of bytes.
        /// </summary>
        /// <param name="target">The sequence of bytes to be replaced.</param>
        /// <param name="replacement">The sequence of bytes to replace with.</param>
        public void Replace(ReadOnlySpan<char> target, ReadOnlySpan<char> replacement)
        {
            if (size < target.Length)
            {
                return;
            }

            int cmp = 0;

            for (int i = 0; i < size; i++)
            {
                if (data[i] == target[cmp])
                {
                    cmp++;

                    if (cmp == target.Length)
                    {
                        var idx = i - cmp + 1;
                        var newSize = size - cmp + replacement.Length;

                        Grow(newSize);

                        if (i + 1 != size)
                        {
                            // copy data forward/backwards to ensure no data is lost or is additionally there where no data should be.
                            int delta = replacement.Length - target.Length;
                            MemcpyT(data + idx + target.Length, data + idx + replacement.Length, size - delta);
                        }

                        for (int j = 0; j < replacement.Length; j++)
                        {
                            data[idx + j] = (byte)replacement[j];
                        }

                        size = newSize;
                        i = idx + replacement.Length;
                        cmp = 0;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="StdString"/> that is a substring of the current string, starting at the specified index and having the specified length.
        /// </summary>
        /// <param name="index">The index in the current string where the substring starts.</param>
        /// <param name="length">The length of the substring to create.</param>
        /// <returns>A new <see cref="StdString"/> representing the substring.</returns>
        public readonly StdString SubString(int index, int length)
        {
            StdString @string = new(length);
            @string.Capacity = length;
            @string.size = length;
            MemcpyT(data + index, @string.data, length);
            return @string;
        }

        /// <summary>
        /// Creates a new <see cref="StdString"/> that is a substring of the current string, starting at the specified index and extending to the end of the string.
        /// </summary>
        /// <param name="index">The index in the current string where the substring starts.</param>
        /// <returns>A new <see cref="StdString"/> representing the substring.</returns>
        public readonly StdString SubString(int index)
        {
            var length = size - index;
            StdString @string = new(length);
            @string.Capacity = length;
            @string.size = length;
            MemcpyT(data + index, @string.data, length);
            return @string;
        }

        /// <summary>
        /// Searches for the first occurrence of a specified character sequence within the current string, starting at the specified position.
        /// </summary>
        /// <param name="str">The character sequence to search for.</param>
        /// <param name="pos">The starting position for the search.</param>
        /// <returns>The index of the first occurrence of the character sequence, or -1 if it is not found.</returns>
        public readonly int Find(ReadOnlySpan<char> str, int pos)
        {
            fixed (char* pStr = str)
                return Utils.Find(data, size, pStr, str.Length, pos, WCharToCharConverter.Default);
        }

        /// <summary>
        /// Searches for the first occurrence of a specified character sequence within the current string, starting at the specified position.
        /// </summary>
        /// <param name="str">The character sequence to search for.</param>
        /// <param name="pos">The starting position for the search.</param>
        /// <param name="comparer">The comparer to comparing this string.</param>
        /// <returns>The index of the first occurrence of the character sequence, or -1 if it is not found.</returns>
        public readonly int Find(ReadOnlySpan<char> str, int pos, IEqualityComparer<byte> comparer)
        {
            fixed (char* pStr = str)
                return Utils.Find(data, size, pStr, str.Length, pos, WCharToCharConverter.Default, comparer);
        }

        /// <summary>
        /// Searches for the last occurrence of a specified character sequence within the current string, starting at the specified position.
        /// </summary>
        /// <param name="str">The character sequence to search for.</param>
        /// <param name="pos">The starting position for the search.</param>
        /// <returns>The index of the last occurrence of the character sequence, or -1 if it is not found.</returns>
        public int FindLast(ReadOnlySpan<char> str, int pos)
        {
            if (str.Length > size - pos)
            {
                return -1;
            }

            int cmp = str.Length - 1;
            for (int i = pos; i >= 0; i--)
            {
                if (data[i] == str[cmp])
                {
                    cmp--;
                    if (cmp == 0)
                    {
                        return i;
                    }
                }
                else
                {
                    cmp = str.Length - 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Searches for the first occurrence of a specified byte sequence within the current string, starting at the specified position.
        /// </summary>
        /// <param name="str">The byte sequence to search for.</param>
        /// <param name="pos">The starting position for the search.</param>
        /// <returns>The index of the first occurrence of the byte sequence, or -1 if it is not found.</returns>
        public readonly int Find(ReadOnlySpan<byte> str, int pos)
        {
            fixed (byte* pStr = str)
                return Utils.Find(data, size, pStr, str.Length, pos);
        }

        /// <summary>
        /// Searches for the last occurrence of a specified byte sequence within the current string, starting at the specified position.
        /// </summary>
        /// <param name="str">The byte sequence to search for.</param>
        /// <param name="pos">The starting position for the search.</param>
        /// <returns>The index of the last occurrence of the byte sequence, or -1 if it is not found.</returns>
        public int FindLast(ReadOnlySpan<byte> str, int pos)
        {
            if (str.Length > size - pos)
            {
                return -1;
            }

            int cmp = str.Length - 1;
            for (int i = pos; i >= 0; i--)
            {
                if (data[i] == str[cmp])
                {
                    cmp--;
                    if (cmp == 0)
                    {
                        return i;
                    }
                }
                else
                {
                    cmp = str.Length - 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Swaps the contents of this string with another string.
        /// </summary>
        /// <param name="other">The other string to swap with.</param>
        public void Swap(ref StdString other)
        {
            var tmpData = data;
            data = other.data;
            other.data = tmpData;
            (other.size, size) = (size, other.size);
            (other.capacity, capacity) = (capacity, other.capacity);
        }

        /// <summary>
        /// Creates a new string with the same content as the current string.
        /// </summary>
        /// <returns>A new <see cref="StdString"/> containing a copy of the current string's data.</returns>
        public readonly StdString Clone()
        {
            StdString @string = new(size);
            @string.Capacity = size;
            @string.size = size;
            MemcpyT(data, @string.data, size);
            return @string;
        }

        /// <summary>
        /// Converts the string to a wide string (StdWString) using UTF-8 encoding.
        /// </summary>
        /// <returns>A wide string representation of the current string.</returns>
        public StdWString ToWString()
        {
            StdWString @string = new(size);
            @string.Resize(size);
            for (int i = 0; i < size; i++)
            {
                @string[i] = (char)this[i];
            }
            return @string;
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> that represents the data in the current <see cref="StdString"/>.
        /// </summary>
        /// <returns>A <see cref="Span{T}"/> that represents the data.</returns>
        public readonly Span<byte> AsSpan()
        {
            return new Span<byte>(data, size);
        }

        /// <summary>
        /// Returns a <see cref="ReadOnlySpan{T}"/> that represents the data in the current <see cref="StdString"/>.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> that represents the data.</returns>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return new ReadOnlySpan<byte>(data, size);
        }

        /// <summary>
        /// Concatenates a byte to the end of the current <see cref="StdString"/>.
        /// </summary>
        /// <param name="str">The <see cref="StdString"/> to concatenate to.</param>
        /// <param name="c">The byte to concatenate.</param>
        /// <returns>The concatenated <see cref="StdString"/>.</returns>
        public static StdString operator +(StdString str, byte c)
        {
            str.Append(c);
            return str;
        }

        /// <summary>
        /// Compares two <see cref="StdString"/> objects for equality.
        /// </summary>
        /// <param name="str1">The first <see cref="StdString"/> to compare.</param>
        /// <param name="str2">The second <see cref="StdString"/> to compare.</param>
        /// <returns><c>true</c> if the two strings are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(StdString str1, StdString str2)
        {
            return str1.Equals(str2);
        }

        /// <summary>
        /// Compares two <see cref="StdString"/> objects for inequality.
        /// </summary>
        /// <param name="str1">The first <see cref="StdString"/> to compare.</param>
        /// <param name="str2">The second <see cref="StdString"/> to compare.</param>
        /// <returns><c>true</c> if the two strings are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(StdString str1, StdString str2)
        {
            return !str1.Equals(str2);
        }

        /// <summary>
        /// Compares a <see cref="StdString"/> object with a <see cref="string"/> for equality.
        /// </summary>
        /// <param name="str1">The <see cref="StdString"/> to compare.</param>
        /// <param name="str2">The <see cref="string"/> to compare.</param>
        /// <returns><c>true</c> if the two strings are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(StdString str1, string str2)
        {
            return str1.Equals(str2);
        }

        /// <summary>
        /// Compares a <see cref="StdString"/> object with a <see cref="string"/> for inequality.
        /// </summary>
        /// <param name="str1">The <see cref="StdString"/> to compare.</param>
        /// <param name="str2">The <see cref="string"/> to compare.</param>
        /// <returns><c>true</c> if the two strings are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(StdString str1, string str2)
        {
            return !str1.Equals(str2);
        }

        /// <summary>
        /// Implicitly converts a <see cref="StdString"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="str">The <see cref="StdString"/> to convert.</param>
        public static implicit operator StdString(string str)
        {
            return new StdString(str);
        }

        /// <summary>
        /// Implicitly converts a <see cref="StdString"/> to a <see cref="Span{T}"/>.
        /// </summary>
        /// <param name="str">The <see cref="StdString"/> to convert.</param>
        public static implicit operator Span<byte>(StdString str)
        {
            return str.AsSpan();
        }

        /// <summary>
        /// Implicitly converts a <see cref="StdString"/> to a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="str">The <see cref="StdString"/> to convert.</param>
        public static implicit operator ReadOnlySpan<byte>(StdString str)
        {
            return str.AsReadOnlySpan();
        }

        /// <summary>
        /// Releases the memory associated with the string.
        /// </summary>
        public void Release()
        {
            if (data != null)
            {
                Free(data);
                data = null;
            }

            capacity = 0;
            size = 0;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current string.
        /// </summary>
        /// <param name="obj">The object to compare with the current string.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is StdString stdString)
            {
                return Equals(stdString);
            }
            if (obj is string str)
            {
                return Equals(str);
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for the string.
        /// </summary>
        /// <returns>The hash code for the string.</returns>
        public override int GetHashCode()
        {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            HashCode hashCode = new();
            for (int i = 0; i < size; i++)
            {
                hashCode.Add(data[i]);
            }
            return hashCode.ToHashCode();
#else
            int hash = 17;
            for (int i = 0; i < size; i++)
            {
                hash = hash * 31 + data[i].GetHashCode();
            }
            return hash;
#endif
        }

        /// <summary>
        /// Converts the string to a C# string using UTF-8 encoding.
        /// </summary>
        /// <returns>A C# string representing the current string's data.</returns>
        public override readonly string ToString()
        {
            return Encoding.UTF8.GetString(data, size);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the bytes of the string.
        /// </summary>
        /// <returns>An enumerator for the bytes of the string.</returns>
        public readonly IEnumerator<byte> GetEnumerator()
        {
            return new Enumerator(this, false);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the bytes of the string.
        /// </summary>
        /// <returns>An enumerator for the bytes of the string.</returns>
        public readonly IEnumerator<byte> Begin()
        {
            return new Enumerator(this, false);
        }

        /// <summary>
        /// Returns a reverse enumerator that iterates through the bytes of the string.
        /// </summary>
        /// <returns>A reverse enumerator for the bytes of the string.</returns>
        public readonly IEnumerator<byte> RBegin()
        {
            return new Enumerator(this, true);
        }

        /// <summary>
        /// Enumerator for iterating through the bytes of the string.
        /// </summary>
        public struct Enumerator : IEnumerator<byte>
        {
            private readonly byte* pointer;
            private readonly int size;
            private readonly bool reverse;
            private int currentIndex;

            internal Enumerator(StdString str, bool reverse)
            {
                pointer = str.data;
                size = str.size;
                currentIndex = reverse ? size : -1;
                this.reverse = reverse;
            }

            /// <inheritdoc/>
            public byte Current => pointer[currentIndex];

            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public readonly void Dispose()
            {
                // Enumerator does not own resources, so nothing to dispose.
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (reverse)
                {
                    if (currentIndex > 0)
                    {
                        currentIndex--;
                        return true;
                    }
                }
                else if (currentIndex < size - 1)
                {
                    currentIndex++;
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                currentIndex = 0;
            }
        }
    }
}