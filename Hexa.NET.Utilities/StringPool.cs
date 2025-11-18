#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities
{
    using System;

    public unsafe struct StringPool : IDisposable, IFreeable
    {
        private BumpAllocator allocator;
        private UnsafeHashSet<StringSpan> strings;

        public StringSpan Take(ReadOnlySpan<byte> span)
        {
            uint length = (uint)span.Length + 1;
            byte* ptr = (byte*)allocator.Alloc(length, 1);
            span.CopyTo(new Span<byte>(ptr, span.Length));
            ptr[span.Length] = 0;
            StringSpan stringSpan = new(ptr, (int)length - 1);
            var res = strings.AddIt(stringSpan);
            if (!res.Added)
            {
                allocator.Free(ptr, length);
                return res.Entry->Value;
            }

            return stringSpan;
        }

        public void Dispose()
        {
            allocator.Dispose();
            strings.Release();
        }

        public void Release()
        {
            Dispose();
        }
    }
}
#endif