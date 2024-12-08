namespace Hexa.NET.Utilities.Text
{
    using System;
    using System.Globalization;

    public unsafe ref struct StrBuilder
    {
        public int Index;
        public byte* Buffer;
        public int Count;

        public StrBuilder(int index, byte* buffer, int count)
        {
            Index = index;
            Buffer = buffer;
            Count = count;
        }

        public StrBuilder(byte* buffer, int count)
        {
            Buffer = buffer;
            Count = count;
        }

        public void Append(string text)
        {
            Index += Utf8Formatter.ConvertUtf16ToUtf8(text, Buffer + Index, Count - Index);
        }

        public void Append(ReadOnlySpan<byte> text)
        {
            byte* start = Buffer + Index;
            byte* ptr = start;
            byte* end = Buffer + Count;
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
            Index += Utf8Formatter.ConvertUtf16ToUtf8(c, Buffer + Index, Count - Index);
        }

        public void Append(byte c)
        {
            if (Index + 1 >= Count) return;
            Buffer[Index++] = c;
        }

        public void Append(double value, int digits = -1)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, digits);
        }

        public void Append(float value, int digits = -1)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, digits);
        }

        public void Append(int value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(uint value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(long value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(ulong value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(short value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(ushort value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(DateTime value, string format, CultureInfo cultureInfo)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, format, cultureInfo);
        }

        public void Append(DateTime value, string format)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, format);
        }

        public void Append(DateTime value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void Append(TimeSpan value, string format, CultureInfo cultureInfo)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, format, cultureInfo);
        }

        public void Append(TimeSpan value, string format)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index, format);
        }

        public void Append(TimeSpan value)
        {
            Index += Utf8Formatter.Format(value, Buffer + Index, Count - Index);
        }

        public void AppendByteSize(long value, bool addSuffixSpace, int digits = -1)
        {
            Index += Utf8Formatter.FormatByteSize(Buffer + Index, Count - Index, value, addSuffixSpace, digits);
        }

        public void End()
        {
            if (Index >= Count)
            {
                Index = Count - 1;
            }
            Buffer[Index] = 0;
        }

        public void Reset()
        {
            Index = 0;
        }

        public static implicit operator byte*(StrBuilder builder) => builder.Buffer;
    }
}