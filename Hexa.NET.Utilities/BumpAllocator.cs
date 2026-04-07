#if NET5_0_OR_GREATER

namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct BumpAllocator : IDisposable, IFreeable
    {
        public const nuint PageSize = 4096;

        private struct MemoryBlock
        {
            public MemoryBlock* Next;
            public nuint Used;
            public nuint Size;

            public MemoryBlock(MemoryBlock* next, nuint size)
            {
                Next = next;
                Size = size;
            }

            public static MemoryBlock* Create(nuint size, MemoryBlock* next)
            {
                size = AlignmentHelper.AlignUp(size + (nuint)sizeof(MemoryBlock), PageSize);
                var mem = (byte*)NativeMemory.AlignedAlloc(size, PageSize) + size;
                MemoryBlock* block = ((MemoryBlock*)mem) - 1;
                *block = new(next, size - (nuint)sizeof(MemoryBlock));
                return block;
            }

            public nuint Alloc(nuint size, nuint alignment)
            {
                var offset = Used;
                var alignedOffset = AlignmentHelper.AlignUp(offset, alignment);
                var newUsed = alignedOffset + size;
                if (newUsed > Size)
                {
                    return nuint.MaxValue;
                }
                Used = newUsed;
                return alignedOffset;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte* GetBasePointer(MemoryBlock* block)
            {
                return (byte*)block - block->Size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void* GetPointer(MemoryBlock* block, nuint offset)
            {
                return GetBasePointer(block) + offset;
            }

            public static void Destroy(MemoryBlock* block)
            {
                NativeMemory.AlignedFree(GetBasePointer(block));
            }
        }

        private MemoryBlock* bottom;
        private MemoryBlock* top;
        private MemoryBlock* freeListTop;

        private MemoryBlock* CreateBlock(nuint sizeMin)
        {
            MemoryBlock* block;

            if (freeListTop != null)
            {
                block = freeListTop;
                freeListTop = freeListTop->Next;
                block->Next = top;
                top = block;
                return block;
            }

            block = MemoryBlock.Create(Math.Max(sizeMin, PageSize), top);
            top = block;
            if (bottom == null)
            {
                bottom = block;
            }

            return block;
        }

        public T* Alloc<T>(nuint count, nuint alignment) where T : unmanaged
        {
            return (T*)Alloc(count * (nuint)sizeof(T), alignment);
        }

        public void* Alloc(nuint size, nuint alignment = 8)
        {
            while (true)
            {
                var currentTail = top;
                if (currentTail != null)
                {
                    var offs = currentTail->Alloc(size, alignment);
                    if (offs != nuint.MaxValue)
                    {
                        return MemoryBlock.GetPointer(currentTail, offs);
                    }
                }

                CreateBlock(size);
            }
        }

        public void Free(void* ptr, nuint size)
        {
            var basePtr = MemoryBlock.GetBasePointer(top);
            var endPtr = basePtr + top->Used;
            if ((byte*)ptr + size == endPtr)
            {
                top->Used -= size;
            }
        }

        public void Reset()
        {
            var curr = top;
            while (curr != null)
            {
                curr->Used = 0;
                curr = curr->Next;
            }
            if (bottom != null)
            {
                bottom->Next = freeListTop;
            }
            freeListTop = top;
            top = null;
            bottom = null;
        }

        public void ReleaseAll()
        {
            DestroyList(top);
            DestroyList(freeListTop);
            top = null;
            bottom = null;
            freeListTop = null;
        }

        private static void DestroyList(MemoryBlock* curr)
        {
            while (curr != null)
            {
                var next = curr->Next;
                MemoryBlock.Destroy(curr);
                curr = next;
            }
        }

        public void Dispose()
        {
            ReleaseAll();
        }

        public void Release()
        {
            ReleaseAll();
        }
    }
}

#endif