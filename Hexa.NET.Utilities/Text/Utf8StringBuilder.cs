namespace Hexa.NET.Utilities.Text
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;

    public unsafe class Utf8StringBuilder : IDisposable
    {
        private const int DefaultCapacity = 1024 * 16;
        public int Index;
        private byte* buffer;
        private int capacity;

        public Utf8StringBuilder()
        {
            buffer = (byte*)Alloc(DefaultCapacity);
            capacity = DefaultCapacity;
        }

        public static readonly Utf8StringBuilder Shared = new();

        public byte* Buffer => buffer;

        public int Capacity
        {
            get => capacity;
            set
            {
                buffer = (byte*)ReAlloc(buffer, value);
                capacity = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int newCapacity)
        {
            if (newCapacity > capacity)
            {
                Capacity = Math.Max(newCapacity, capacity * 2);
            }
        }

        public void Append(string text)
        {
            int result = 0;
            while (result == 0)
            {
                result = Utf8Formatter.ConvertUtf16ToUtf8(text, buffer + Index, capacity - Index);
                if (result == 0) EnsureCapacity(capacity + text.Length * 2);
            }
            Index += result;
        }

        public void Append(ReadOnlySpan<byte> text)
        {
            EnsureCapacity(capacity + text.Length);
            byte* start = buffer + Index;
            byte* ptr = start;
            byte* end = buffer + capacity;
            int i = 0;
            while (ptr != end && i < text.Length)
            {
                *ptr = text[i];
                ptr++; i++;
            }
            int written = (int)(ptr - start);
            Index += written;
        }

        public void Append(char c)
        {
            int result = 0;
            while (result == 0)
            {
                result = Utf8Formatter.ConvertUtf16ToUtf8(c, buffer + Index, capacity - Index);
                if (result == 0) EnsureCapacity(capacity + 2);
            }
            Index += result;
        }

        public void Append(byte c)
        {
            EnsureCapacity(capacity + 1);
            if (Index + 1 >= capacity) return;
            buffer[Index++] = c;
        }

        public void Append(double value, int digits = -1)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, digits);
        }

        public void Append(float value, int digits = -1)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, digits);
        }

        public void Append(int value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(uint value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(long value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(ulong value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(short value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(ushort value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(DateTime value, string format, CultureInfo cultureInfo)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, format, cultureInfo);
        }

        public void Append(DateTime value, string format)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, format);
        }

        public void Append(DateTime value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void Append(TimeSpan value, string format, CultureInfo cultureInfo)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, format, cultureInfo);
        }

        public void Append(TimeSpan value, string format)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index, format);
        }

        public void Append(TimeSpan value)
        {
            Index += Utf8Formatter.Format(value, buffer + Index, capacity - Index);
        }

        public void AppendByteSize(long value, bool addSuffixSpace, int digits = -1)
        {
            Index += Utf8Formatter.FormatByteSize(buffer + Index, capacity - Index, value, addSuffixSpace, digits);
        }

        public void End()
        {
            if (Index >= capacity)
            {
                Index = capacity - 1;
            }
            buffer[Index] = 0;
        }

        public void Reset()
        {
            Index = 0;
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                Free(buffer);
                buffer = null;
                capacity = 0;
                Index = 0;
            }
        }

        public static implicit operator byte*(Utf8StringBuilder builder) => builder.buffer;
    }
}