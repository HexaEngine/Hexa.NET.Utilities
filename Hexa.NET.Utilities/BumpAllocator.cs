#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct BumpAllocator : IDisposable, IFreeable
    {
        public const uint PageSize = 4096;

        private struct MemoryBlock
        {
            public MemoryBlock* Next;
            public uint Used;
            public uint Size;

            public MemoryBlock(MemoryBlock* next, uint size)
            {
                Next = next;
                Size = size;
            }

            public static MemoryBlock* Create(uint size, MemoryBlock* next)
            {
                size = AlignmentHelper.AlignUp(size + (uint)sizeof(MemoryBlock), PageSize);
                var mem = (byte*)NativeMemory.AlignedAlloc(size, PageSize) + size;
                MemoryBlock* block = ((MemoryBlock*)mem) - 1;
                *block = new(next, size - (uint)sizeof(MemoryBlock));
                return block;
            }

            public uint Alloc(uint size, uint alignment)
            {
                var offset = Used;
                var alignedOffset = AlignmentHelper.AlignUp(offset, alignment);
                var newUsed = alignedOffset + size;
                if (newUsed > Size)
                {
                    return uint.MaxValue;
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
            public static void* GetPointer(MemoryBlock* block, uint offset)
            {
                return GetBasePointer(block) + offset;
            }


            public static void Destroy(MemoryBlock* block)
            {
                NativeMemory.AlignedFree(GetBasePointer(block));
            }
        }

        private MemoryBlock* head;
        private MemoryBlock* tail;
        private MemoryBlock* freeList;


        private MemoryBlock* CreateBlock(uint sizeMin)
        {
            MemoryBlock* block;

            if (freeList != null)
            {
                block = freeList;
                freeList = freeList->Next;
                block->Next = tail;
                tail = block;
                return block;
            }

            block = MemoryBlock.Create(Math.Max(sizeMin, PageSize), tail);
            tail = block;
            if (head == null)
            {
                head = block;
            }

            return block;

        }

        public void* Alloc(uint size, uint alignment = 8)
        {
            while (true)
            {
                var currentTail = tail;
                if (currentTail != null)
                {
                    var offs = currentTail->Alloc(size, alignment);
                    if (offs != uint.MaxValue)
                    {
                        return MemoryBlock.GetPointer(currentTail, offs);
                    }
                }

                CreateBlock(size);
            }
        }

        public void Free(void* ptr, uint size)
        {
            var basePtr = MemoryBlock.GetBasePointer(tail);
            var endPtr = basePtr + tail->Used;
            if ((byte*)ptr + size == endPtr)
            {
                tail->Used -= size;
            }
        }

        public void Reset()
        {
            var curr = tail;
            while (curr != null)
            {
                curr->Used = 0;
                curr = curr->Next;
            }
            freeList = tail;
            tail = null;
            head = null;
        }

        public void ReleaseAll()
        {
            DestroyList(tail);
            DestroyList(freeList);
            tail = null;
            head = null;
            freeList = null;
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