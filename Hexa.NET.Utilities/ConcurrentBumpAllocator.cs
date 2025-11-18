#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct ConcurrentBumpAllocator : IDisposable, IFreeable
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
                uint offset, alignedOffset, newUsed;
                do
                {
                    offset = Volatile.Read(ref Used);
                    alignedOffset = AlignmentHelper.AlignUp(offset, alignment);
                    newUsed = alignedOffset + size;
                    if (newUsed > Size)
                    {
                        return uint.MaxValue;
                    }

                } while (Interlocked.CompareExchange(ref Used, newUsed, offset) != offset);

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

        private volatile MemoryBlock* head;
        private volatile MemoryBlock* tail;
        private volatile MemoryBlock* freeList;
        private int token;

        private bool AcquireLock(bool forceAcquire = false)
        {
            int failValue = forceAcquire ? 1 : 0;
            // this is a refill lock, do not interpret it wrong.
            int toSet = 1;
            while (Interlocked.CompareExchange(ref token, toSet, 0) != 0)
            {
                Thread.Yield();
                toSet = failValue;
            }
            return toSet == 1;
        }

        private void ReleaseLock()
        {
            Interlocked.Exchange(ref token, 0);
        }

        private MemoryBlock* CreateBlock(uint sizeMin)
        {
            var oldTail = tail;

            if (!AcquireLock())
            {
                return tail;
            }
            try
            {
                MemoryBlock* block;
                if (tail != oldTail)
                {
                    return tail;
                }

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
            finally
            {

                ReleaseLock();
            }

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

        public void Reset()
        {
            AcquireLock(true);
            try
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
            finally
            {
                ReleaseLock();
            }
        }

        public void ReleaseAll()
        {
            AcquireLock(true);
            try
            {
                DestroyList(tail);
                DestroyList(freeList);
                tail = null;
                head = null;
                freeList = null;
            }
            finally
            {
                ReleaseLock();
            }
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