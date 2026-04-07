#if NET7_0_OR_GREATER

namespace Hexa.NET.Utilities
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public unsafe struct ConcurrentBumpAllocator : IDisposable, IFreeable
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
                nuint offset, alignedOffset, newUsed;
                do
                {
                    offset = Volatile.Read(ref Used);
                    alignedOffset = AlignmentHelper.AlignUp(offset, alignment);
                    newUsed = alignedOffset + size;
                    if (newUsed > Size)
                    {
                        return nuint.MaxValue;
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
            public static void* GetPointer(MemoryBlock* block, nuint offset)
            {
                return GetBasePointer(block) + offset;
            }

            public static void Destroy(MemoryBlock* block)
            {
                NativeMemory.AlignedFree(GetBasePointer(block));
            }
        }

        private volatile MemoryBlock* bottom;
        private volatile MemoryBlock* top;
        private volatile MemoryBlock* freeListTop;
        private int token;

        private bool AcquireRefillGate(bool forceAcquire = false)
        {
            // A specialized spinlock for refilling.
            // Reasoning: Using a normal mutex/futex would cause threads to serialize on this spot, even if the refill already happened.
            // With this approach, only one thread will do the refill, but other threads wait and upon release, they will continue without each aquiring the refill lock.

            // Used like:
            // if (!AcquireRefillGate()) return
            // <critical section> // only one thread will execute this, but others will wait for the refill to complete and then continue without entering the critical section
            // SignalRefillGate();

            int failValue = forceAcquire ? 1 : 0;
            int toSet = 1;
            while (Interlocked.CompareExchange(ref token, toSet, 0) != 0)
            {
                Thread.SpinWait(10);
                toSet = failValue;
            }
            return toSet == 1;
        }

        private void SignalRefillGate()
        {
            Interlocked.Exchange(ref token, 0);
        }

        private MemoryBlock* CreateBlock(nuint sizeMin)
        {
            var oldTail = top;

            if (!AcquireRefillGate())
            {
                return top;
            }
            try
            {
                MemoryBlock* block;
                if (top != oldTail)
                {
                    return top;
                }

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
            finally
            {
                SignalRefillGate();
            }
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

        /// <summary>
        /// Resets the allocator to its initial state, clearing all allocated elements and preparing the structure for
        /// reuse.
        /// </summary>
        /// <remarks>
        /// This method is <strong>not thread-safe</strong> and should be called when no other threads are accessing the allocator.
        /// </remarks>
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

        /// <summary>
        /// Releases all resources held by the current instance and resets its internal state.
        /// </summary>
        /// <remarks>
        /// This method is <strong>not thread-safe</strong> and should be called when no other threads are accessing the allocator.
        /// </remarks>
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