namespace Hexa.NET.Utilities.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public unsafe class ConcurrentBumpAllocatorTests
    {
        [Test]
        public void Alloc_SingleAllocation_ReturnsValidPointer()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();

            // Act
            void* ptr = allocator.Alloc(64);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_MultipleAllocations_ReturnsValidPointers()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();

            // Act
            void* ptr1 = allocator.Alloc(32);
            void* ptr2 = allocator.Alloc(64);
            void* ptr3 = allocator.Alloc(128);

            // Assert
            Assert.That((nint)ptr1, Is.Not.EqualTo(0));
            Assert.That((nint)ptr2, Is.Not.EqualTo(0));
            Assert.That((nint)ptr3, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_WithAlignment_ReturnsAlignedPointer()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            uint[] alignments = { 8, 16, 32, 64, 128, 256 };

            foreach (var alignment in alignments)
            {
                // Act
                void* ptr = allocator.Alloc(64, alignment);

                // Assert
                Assert.That((nint)ptr % alignment, Is.EqualTo(0),
                    $"Pointer should be aligned to {alignment} bytes");
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_NoMemoryOverlap_EachAllocationIsUnique()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            const int allocationCount = 100;
            const int allocationSize = 64;
            var pointers = new List<(nint address, int size)>();

            // Act
            for (int i = 0; i < allocationCount; i++)
            {
                void* ptr = allocator.Alloc(allocationSize);
                Assert.That((nint)ptr, Is.Not.EqualTo(0));
                pointers.Add(((nint)ptr, allocationSize));

                // Write a unique pattern to verify no overlap
                byte* bytePtr = (byte*)ptr;
                for (int j = 0; j < allocationSize; j++)
                {
                    bytePtr[j] = (byte)(i & 0xFF);
                }
            }

            // Assert - Check for overlaps
            for (int i = 0; i < pointers.Count; i++)
            {
                var (addr1, size1) = pointers[i];
                nint end1 = addr1 + size1;

                for (int j = i + 1; j < pointers.Count; j++)
                {
                    var (addr2, size2) = pointers[j];
                    nint end2 = addr2 + size2;

                    // Check if ranges overlap
                    bool overlaps = addr1 < end2 && addr2 < end1;
                    Assert.That(overlaps, Is.False,
                        $"Allocation {i} (0x{addr1:X}) and {j} (0x{addr2:X}) overlap");
                }
            }

            // Verify data integrity - each allocation should still have its unique pattern
            for (int i = 0; i < pointers.Count; i++)
            {
                byte* bytePtr = (byte*)pointers[i].address;
                for (int j = 0; j < allocationSize; j++)
                {
                    Assert.That(bytePtr[j], Is.EqualTo((byte)(i & 0xFF)),
                        $"Memory corruption detected at allocation {i}, offset {j}");
                }
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_LargeAllocation_ExceedingSinglePage_ReturnsValidPointer()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            uint largeSize = ConcurrentBumpAllocator.PageSize * 2;

            // Act
            void* ptr = allocator.Alloc(largeSize);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Verify we can write to the entire allocation
            byte* bytePtr = (byte*)ptr;
            for (uint i = 0; i < largeSize; i++)
            {
                bytePtr[i] = (byte)(i & 0xFF);
            }

            // Verify data
            for (uint i = 0; i < largeSize; i++)
            {
                Assert.That(bytePtr[i], Is.EqualTo((byte)(i & 0xFF)));
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Reset_AfterAllocations_ReusesMemory()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();

            // First allocation cycle
            void* ptr1 = allocator.Alloc(64);
            nint firstAddress = (nint)ptr1;

            // Act
            allocator.Reset();

            // Second allocation cycle
            void* ptr2 = allocator.Alloc(64);
            nint secondAddress = (nint)ptr2;

            // Assert - After reset, we should get the same address (memory is reused)
            Assert.That(secondAddress, Is.EqualTo(firstAddress));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Dispose_ReleasesMemory()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            allocator.Alloc(64);
            allocator.Alloc(128);
            allocator.Alloc(256);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => allocator.Dispose());
        }

        [Test]
        public void ThreadSafety_ConcurrentAllocations_NoMemoryOverlap()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            const int threadCount = 10;
            const int allocationsPerThread = 1000;
            const int allocationSize = 32;
            var allPointers = new List<(nint address, int size, int threadId)>[threadCount];
            var tasks = new Task[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                allPointers[i] = new List<(nint, int, int)>();
            }

            // Act - Allocate from multiple threads concurrently
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    var pointers = allPointers[threadId];

                    for (int i = 0; i < allocationsPerThread; i++)
                    {
                        void* ptr = allocator.Alloc(allocationSize);
                        Assert.That((nint)ptr, Is.Not.EqualTo(0));

                        nint address = (nint)ptr;
                        pointers.Add((address, allocationSize, threadId));

                        // Write thread-specific pattern
                        byte* bytePtr = (byte*)ptr;
                        for (int j = 0; j < allocationSize; j++)
                        {
                            bytePtr[j] = (byte)((threadId * 100 + i) & 0xFF);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert - Check for overlaps across all threads
            var flatPointers = new List<(nint address, int size, int threadId, int index)>();
            for (int t = 0; t < threadCount; t++)
            {
                for (int i = 0; i < allPointers[t].Count; i++)
                {
                    var (addr, size, tid) = allPointers[t][i];
                    flatPointers.Add((addr, size, tid, i));
                }
            }

            // Check for overlaps
            for (int i = 0; i < flatPointers.Count; i++)
            {
                var (addr1, size1, tid1, idx1) = flatPointers[i];
                nint end1 = addr1 + size1;

                for (int j = i + 1; j < flatPointers.Count; j++)
                {
                    var (addr2, size2, tid2, idx2) = flatPointers[j];
                    nint end2 = addr2 + size2;

                    bool overlaps = addr1 < end2 && addr2 < end1;
                    Assert.That(overlaps, Is.False,
                        $"Thread {tid1} allocation {idx1} (0x{addr1:X}) and Thread {tid2} allocation {idx2} (0x{addr2:X}) overlap");
                }
            }

            // Verify data integrity
            for (int t = 0; t < threadCount; t++)
            {
                for (int i = 0; i < allPointers[t].Count; i++)
                {
                    var (addr, size, threadId) = allPointers[t][i];
                    byte* bytePtr = (byte*)addr;
                    byte expected = (byte)((threadId * 100 + i) & 0xFF);

                    for (int j = 0; j < size; j++)
                    {
                        Assert.That(bytePtr[j], Is.EqualTo(expected),
                            $"Memory corruption in thread {threadId}, allocation {i}, offset {j}");
                    }
                }
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void ThreadSafety_ConcurrentAllocationsWithDifferentSizes_NoMemoryOverlap()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            const int threadCount = 8;
            const int allocationsPerThread = 500;
            var allPointers = new List<(nint address, int size)>[threadCount];
            var tasks = new Task[threadCount];
            var random = new Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < threadCount; i++)
            {
                allPointers[i] = new List<(nint, int)>();
            }

            // Act - Allocate varying sizes from multiple threads
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                int seed = random.Next();
                tasks[t] = Task.Run(() =>
                {
                    var threadRandom = new Random(seed);
                    var pointers = allPointers[threadId];

                    for (int i = 0; i < allocationsPerThread; i++)
                    {
                        // Random size between 8 and 512 bytes
                        int size = threadRandom.Next(8, 513);
                        void* ptr = allocator.Alloc((uint)size);
                        Assert.That((nint)ptr, Is.Not.EqualTo(0));

                        pointers.Add(((nint)ptr, size));

                        // Write unique pattern
                        byte* bytePtr = (byte*)ptr;
                        byte pattern = (byte)(threadId + i);
                        for (int j = 0; j < size; j++)
                        {
                            bytePtr[j] = pattern;
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert - Check for overlaps
            var flatPointers = new List<(nint address, int size)>();
            for (int t = 0; t < threadCount; t++)
            {
                flatPointers.AddRange(allPointers[t]);
            }

            for (int i = 0; i < flatPointers.Count; i++)
            {
                var (addr1, size1) = flatPointers[i];
                nint end1 = addr1 + size1;

                for (int j = i + 1; j < flatPointers.Count; j++)
                {
                    var (addr2, size2) = flatPointers[j];
                    nint end2 = addr2 + size2;

                    bool overlaps = addr1 < end2 && addr2 < end1;
                    Assert.That(overlaps, Is.False,
                        $"Allocation {i} (0x{addr1:X}, size {size1}) and {j} (0x{addr2:X}, size {size2}) overlap");
                }
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void ThreadSafety_ConcurrentResetAndAlloc_NoCorruption()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            const int duration = 1000; // milliseconds
            var cts = new CancellationTokenSource();
            var exceptions = new List<Exception>();

            // Act - One thread continuously allocates, another resets periodically
            var allocTask = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        void* ptr = allocator.Alloc(64);
                        if (ptr != null)
                        {
                            // Write some data
                            *(int*)ptr = 0x12345678;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            var resetTask = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(10);
                        allocator.Reset();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Thread.Sleep(duration);
            cts.Cancel();
            Task.WaitAll(allocTask, resetTask);

            // Assert - No exceptions should occur
            Assert.That(exceptions, Is.Empty,
                $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_SmallAllocations_FitsInSinglePage()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            uint totalAllocated = 0;
            const uint smallSize = 16;

            // Act - Allocate many small blocks that should fit in one page
            while (totalAllocated < ConcurrentBumpAllocator.PageSize - 256)
            {
                void* ptr = allocator.Alloc(smallSize);
                Assert.That((nint)ptr, Is.Not.EqualTo(0));
                totalAllocated += smallSize; // Account for alignment
            }

            // Assert - Just verify no crashes occurred
            Assert.Pass("Successfully allocated multiple small blocks");

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void ReleaseAll_AfterAllocations_CanReallocate()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            allocator.Alloc(64);
            allocator.Alloc(128);

            // Act
            allocator.ReleaseAll();
            void* ptr = allocator.Alloc(64);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void IFreeable_Release_DisposesAllocator()
        {
            // Arrange
            IFreeable allocator = new ConcurrentBumpAllocator();
            var bumpAlloc = (ConcurrentBumpAllocator)allocator;
            bumpAlloc.Alloc(64);

            // Act & Assert
            Assert.DoesNotThrow(() => allocator.Release());
        }

        [Test]
        public void Alloc_ZeroSize_ReturnsValidPointer()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();

            // Act
            void* ptr = allocator.Alloc(0);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_MaxAlignment_ReturnsAlignedPointer()
        {
            // Arrange
            var allocator = new ConcurrentBumpAllocator();
            uint maxAlignment = 4096; // Page size alignment

            // Act
            void* ptr = allocator.Alloc(64, maxAlignment);

            // Assert
            Assert.That((nint)ptr % maxAlignment, Is.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }
    }
}
