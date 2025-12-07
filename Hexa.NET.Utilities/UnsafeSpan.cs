namespace Hexa.NET.Utilities
{
    using Hexa.NET.Utilities.Hashing;
    using System.Text;

    public unsafe struct UnsafeSpan<T> where T : unmanaged
    {
        public T* Pointer;
        public nuint Length;

        public UnsafeSpan(T* pointer, nuint length)
        {
            Pointer = pointer;
            Length = length;
        }

        public ref T this[nuint index]
        {
            get
            {
                if (index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
                return ref Pointer[index];
            }
        }

#if NET5_0_OR_GREATER

        public readonly UnsafeSpan<T> this[Range range]
        {
            get
            {
                (int start, int length) = range.GetOffsetAndLength((int)Length);
                return new UnsafeSpan<T>(Pointer + start, (nuint)length);
            }
        }

        public readonly UnsafeSpan<T> this[Index index]
        {
            get
            {
                nuint idx = (nuint)index.GetOffset((int)Length);
                return new UnsafeSpan<T>(Pointer + idx, 1);
            }
        }

#endif

        public readonly UnsafeSpan<T> Slice(nuint start)
        {
            if (start > Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new UnsafeSpan<T>(Pointer + start, Length - start);
        }

        public readonly UnsafeSpan<T> Slice(nuint start, nuint length)
        {
            if (start + length > Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            return new UnsafeSpan<T>(Pointer + start, length);
        }

        public override readonly int GetHashCode()
        {
            return MurmurHash3.Hash64((byte*)Pointer, Length * (nuint)sizeof(T)).GetHashCode();
        }

        public readonly bool Equals(in UnsafeSpan<T> other)
        {
            if (Length != other.Length)
            {
                return false;
            }

            return MemcmpT(Pointer, other.Pointer, Length) == 0;
        }

        public override bool Equals(object? obj)
        {
            return obj is UnsafeSpan<T> other && Equals(in other);
        }

        public override readonly string ToString()
        {
            if (typeof(T) == typeof(byte))
            {
                return Encoding.UTF8.GetString((byte*)Pointer, (int)Length);
            }
            else if (typeof(T) == typeof(char))
            {
                return new string((char*)Pointer, 0, (int)Length);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static bool operator ==(UnsafeSpan<T> left, UnsafeSpan<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UnsafeSpan<T> left, UnsafeSpan<T> right)
        {
            return !(left == right);
        }

        public readonly Span<T> AsSpan() => new(Pointer, (int)Length);

        public readonly ReadOnlySpan<T> AsReadOnlySpan() => new(Pointer, (int)Length);

        public static implicit operator Span<T>(in UnsafeSpan<T> span) => span.AsSpan();

        public static implicit operator ReadOnlySpan<T>(in UnsafeSpan<T> span) => span.AsReadOnlySpan();
    }
}