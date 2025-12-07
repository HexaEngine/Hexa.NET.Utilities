namespace Hexa.NET.Utilities
{
    using Hexa.NET.Utilities.Hashing;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

    public unsafe struct StringSpan : IEquatable<StringSpan>, IEnumerable<byte>
    {
        public byte* Ptr;
        public nuint Length;

        public StringSpan(byte* pointer, nuint length)
        {
            Ptr = pointer;
            Length = length;
        }

        /// <summary>
        /// Gets a value indicating whether this span is empty.
        /// </summary>
        public readonly bool IsEmpty => Length == 0;

        /// <summary>
        /// Gets or sets the byte at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the byte to get or set.</param>
        /// <returns>The byte at the specified index.</returns>
        public byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
#if NET8_0_OR_GREATER
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Length);
#else
                if ((uint)index >= (uint)Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
#endif
                return Ptr[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if NET8_0_OR_GREATER
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Length);
#else
                if ((uint)index >= (uint)Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
#endif
                Ptr[index] = value;
            }
        }

        /// <summary>
        /// Returns a span that represents the entire string span.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            return new Span<byte>(Ptr, (int)Length);
        }

        /// <summary>
        /// Returns a read-only span that represents the entire string span.
        /// </summary>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return new ReadOnlySpan<byte>(Ptr, (int)Length);
        }

        /// <summary>
        /// Forms a slice out of the current span starting at a specified index.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <returns>A span that consists of all elements from start to the end of the span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StringSpan Slice(nuint start)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)Length);
#else
            if ((uint)start > (uint)Length)
                throw new ArgumentOutOfRangeException(nameof(start));
#endif
            return new StringSpan(Ptr + start, Length - start);
        }

        /// <summary>
        /// Forms a slice out of the current span that begins at a specified index for a specified length.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The desired length for the slice.</param>
        /// <returns>A span that consists of length elements from the current span starting at start.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly StringSpan Slice(nuint start, nuint length)
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start, (uint)Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)(Length - start));
#else
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
                throw new ArgumentOutOfRangeException();
#endif
            return new StringSpan(Ptr + start, length);
        }

        /// <summary>
        /// Copies the contents of this string span into a destination span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<byte> destination)
        {
            if (Length > (nuint)destination.Length)
                throw new ArgumentException("Destination too short.", nameof(destination));

            AsSpan().CopyTo(destination);
        }

        /// <summary>
        /// Attempts to copy the contents of this string span into a destination span.
        /// </summary>
        /// <param name="destination">The destination span.</param>
        /// <returns>true if the copy succeeded; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<byte> destination)
        {
            if (Length > (nuint)destination.Length)
                return false;

            AsSpan().CopyTo(destination);
            return true;
        }

        /// <summary>
        /// Clears the contents of this string span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Clear()
        {
            AsSpan().Clear();
        }

        /// <summary>
        /// Fills the span with the specified value.
        /// </summary>
        /// <param name="value">The value to fill the span with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Fill(byte value)
        {
            AsSpan().Fill(value);
        }

        /// <summary>
        /// Reports the zero-based index of the first occurrence of the specified byte.
        /// </summary>
        /// <param name="value">The byte to seek.</param>
        /// <returns>The index of the first occurrence of value, or -1 if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(byte value)
        {
            return AsSpan().IndexOf(value);
        }

        /// <summary>
        /// Reports the zero-based index of the first occurrence of the specified sequence.
        /// </summary>
        /// <param name="value">The sequence to seek.</param>
        /// <returns>The index of the first occurrence of value, or -1 if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(ReadOnlySpan<byte> value)
        {
            return AsSpan().IndexOf(value);
        }

        /// <summary>
        /// Reports the zero-based index of the last occurrence of the specified byte.
        /// </summary>
        /// <param name="value">The byte to seek.</param>
        /// <returns>The index of the last occurrence of value, or -1 if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int LastIndexOf(byte value)
        {
            return AsSpan().LastIndexOf(value);
        }

        /// <summary>
        /// Reports the zero-based index of the last occurrence of the specified sequence.
        /// </summary>
        /// <param name="value">The sequence to seek.</param>
        /// <returns>The index of the last occurrence of value, or -1 if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int LastIndexOf(ReadOnlySpan<byte> value)
        {
            return AsSpan().LastIndexOf(value);
        }

        /// <summary>
        /// Determines whether this string span contains the specified value.
        /// </summary>
        /// <param name="value">The value to seek.</param>
        /// <returns>true if the value was found; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(byte value)
        {
            return IndexOf(value) >= 0;
        }

        /// <summary>
        /// Determines whether this string span contains the specified sequence.
        /// </summary>
        /// <param name="value">The sequence to seek.</param>
        /// <returns>true if the value was found; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(ReadOnlySpan<byte> value)
        {
            return IndexOf(value) >= 0;
        }

        /// <summary>
        /// Determines whether this string span starts with the specified sequence.
        /// </summary>
        /// <param name="value">The sequence to compare.</param>
        /// <returns>true if this span starts with value; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool StartsWith(ReadOnlySpan<byte> value)
        {
            return AsSpan().StartsWith(value);
        }

        /// <summary>
        /// Determines whether this string span ends with the specified sequence.
        /// </summary>
        /// <param name="value">The sequence to compare.</param>
        /// <returns>true if this span ends with value; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool EndsWith(ReadOnlySpan<byte> value)
        {
            return AsSpan().EndsWith(value);
        }

        /// <summary>
        /// Reverses the sequence of bytes in the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Reverse()
        {
            AsSpan().Reverse();
        }

        /// <summary>
        /// Compares this string span with another.
        /// </summary>
        /// <param name="other">The string span to compare with.</param>
        /// <returns>A value indicating the relative order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(StringSpan other)
        {
            return AsSpan().SequenceCompareTo(other.AsSpan());
        }

        /// <summary>
        /// Compares this string span with a byte span.
        /// </summary>
        /// <param name="other">The span to compare with.</param>
        /// <returns>A value indicating the relative order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(ReadOnlySpan<byte> other)
        {
            return AsSpan().SequenceCompareTo(other);
        }

        /// <summary>
        /// Determines whether this string span and another span have the same content.
        /// </summary>
        /// <param name="other">The span to compare with.</param>
        /// <returns>true if the contents are identical; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool SequenceEqual(ReadOnlySpan<byte> other)
        {
            return AsSpan().SequenceEqual(other);
        }

        /// <summary>
        /// Converts all ASCII uppercase letters to lowercase in place.
        /// </summary>
        public readonly void ToLowerInPlace()
        {
            for (nuint i = 0; i < Length; i++)
            {
                Ptr[i] = Utils.ToLower(Ptr[i]);
            }
        }

        /// <summary>
        /// Converts all ASCII lowercase letters to uppercase in place.
        /// </summary>
        public readonly void ToUpperInPlace()
        {
            for (nuint i = 0; i < Length; i++)
            {
                Ptr[i] = Utils.ToUpper(Ptr[i]);
            }
        }

        /// <summary>
        /// Trims whitespace from the beginning and end of the string span.
        /// </summary>
        /// <returns>A trimmed string span.</returns>
        public readonly StringSpan Trim()
        {
            nuint start = 0;
            nuint end = Length - 1;

            while (start < Length && IsWhitespace(Ptr[start]))
                start++;

            while (end >= start && IsWhitespace(Ptr[end]))
                end--;

            nuint newLength = end - start + 1;
            return newLength <= 0 ? new StringSpan(null, 0) : new StringSpan(Ptr + start, newLength);
        }

        /// <summary>
        /// Trims whitespace from the beginning of the string span.
        /// </summary>
        /// <returns>A trimmed string span.</returns>
        public readonly StringSpan TrimStart()
        {
            nuint start = 0;
            while (start < Length && IsWhitespace(Ptr[start]))
                start++;

            return start >= Length ? new StringSpan(null, 0) : new StringSpan(Ptr + start, Length - start);
        }

        /// <summary>
        /// Trims whitespace from the end of the string span.
        /// </summary>
        /// <returns>A trimmed string span.</returns>
        public readonly StringSpan TrimEnd()
        {
            nuint end = Length - 1;
            while (end >= 0 && IsWhitespace(Ptr[end]))
                end--;

            nuint newLength = end + 1;
            return newLength <= 0 ? new StringSpan(null, 0) : new StringSpan(Ptr, newLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhitespace(byte b)
        {
            return b == ' ' || b == '\t' || b == '\n' || b == '\r';
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is StringSpan span && Equals(span);
        }

        public readonly bool Equals(StringSpan other)
        {
            return Length == other.Length && StrNCmp(Ptr, other.Ptr, (int)Length) == 0;
        }

        /// <summary>
        /// Determines whether this string span equals the specified byte span.
        /// </summary>
        public readonly bool Equals(ReadOnlySpan<byte> other)
        {
            return SequenceEqual(other);
        }

        public override readonly int GetHashCode()
        {
            return MurmurHash3.Hash64(Ptr, Length).GetHashCode();
        }

        public static bool operator ==(StringSpan left, StringSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringSpan left, StringSpan right)
        {
            return !(left == right);
        }

        public override readonly string ToString()
        {
            return Encoding.UTF8.GetString(Ptr, (int)Length);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the bytes of the string span.
        /// </summary>
        public readonly IEnumerator<byte> GetEnumerator()
        {
            return new Enumerator(this);
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerator for iterating through the bytes of the string span.
        /// </summary>
        public struct Enumerator : IEnumerator<byte>
        {
            private readonly byte* pointer;
            private readonly nuint length;
            private nuint currentIndex;

            internal Enumerator(StringSpan span)
            {
                pointer = span.Ptr;
                length = span.Length;
                currentIndex = unchecked((nuint)(-1));
            }

            /// <inheritdoc/>
            public byte Current => pointer[currentIndex];

            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public readonly void Dispose()
            {
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (currentIndex < length - 1)
                {
                    currentIndex++;
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                currentIndex = unchecked((nuint)(-1));
            }
        }
    }
}