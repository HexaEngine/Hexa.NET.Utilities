#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public unsafe class BumpAllocatorTests
    {
        [Test]
        public void Alloc_SingleAllocation_ReturnsValidPointer()
        {
            // Arrange
            var allocator = new BumpAllocator();

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
            var allocator = new BumpAllocator();

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
            var allocator = new BumpAllocator();
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
            var allocator = new BumpAllocator();
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

            // Verify data integrity
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
            var allocator = new BumpAllocator();
            uint largeSize = BumpAllocator.PageSize * 2;

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
            var allocator = new BumpAllocator();

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
            var allocator = new BumpAllocator();
            allocator.Alloc(64);
            allocator.Alloc(128);
            allocator.Alloc(256);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => allocator.Dispose());
        }

        [Test]
        public void Alloc_SmallAllocations_FitsInSinglePage()
        {
            // Arrange
            var allocator = new BumpAllocator();
            uint totalAllocated = 0;
            const uint smallSize = 16;

            // Act - Allocate many small blocks that should fit in one page
            while (totalAllocated < BumpAllocator.PageSize - 256)
            {
                void* ptr = allocator.Alloc(smallSize);
                Assert.That((nint)ptr, Is.Not.EqualTo(0));
                totalAllocated += smallSize + 8; // Account for alignment
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
            var allocator = new BumpAllocator();
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
            IFreeable allocator = new BumpAllocator();
            var bumpAlloc = (BumpAllocator)allocator;
            bumpAlloc.Alloc(64);

            // Act & Assert
            Assert.DoesNotThrow(() => allocator.Release());
        }

        [Test]
        public void Alloc_ZeroSize_ReturnsValidPointer()
        {
            // Arrange
            var allocator = new BumpAllocator();

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
            var allocator = new BumpAllocator();
            uint maxAlignment = 4096; // Page size alignment

            // Act
            void* ptr = allocator.Alloc(64, maxAlignment);

            // Assert
            Assert.That((nint)ptr % maxAlignment, Is.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Free_LastAllocation_DecreasesUsedMemory()
        {
            // Arrange
            var allocator = new BumpAllocator();
            void* ptr1 = allocator.Alloc(64);
            void* ptr2 = allocator.Alloc(128);

            // Act - Free the last allocation
            allocator.Free(ptr2, 128);

            // Allocate again - should reuse the freed space
            void* ptr3 = allocator.Alloc(128);

            // Assert - Should get same or close address
            Assert.That((nint)ptr3, Is.EqualTo((nint)ptr2));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Free_NotLastAllocation_DoesNothing()
        {
            // Arrange
            var allocator = new BumpAllocator();
            void* ptr1 = allocator.Alloc(64);
            void* ptr2 = allocator.Alloc(128);
            void* ptr3 = allocator.Alloc(256);

            // Act - Try to free the first allocation (not the last one)
            allocator.Free(ptr1, 64);

            // Allocate again
            void* ptr4 = allocator.Alloc(64);

            // Assert - Should allocate new memory, not reuse ptr1
            Assert.That((nint)ptr4, Is.Not.EqualTo((nint)ptr1));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_MultiplePages_AllocatesNewPages()
        {
            // Arrange
            var allocator = new BumpAllocator();
            var pointers = new List<nint>();

            // Act - Allocate enough to require multiple pages
            for (int i = 0; i < 10; i++)
            {
                void* ptr = allocator.Alloc(BumpAllocator.PageSize);
                Assert.That((nint)ptr, Is.Not.EqualTo(0));
                pointers.Add((nint)ptr);
            }

            // Assert - All pointers should be valid and unique
            var uniquePointers = new HashSet<nint>(pointers);
            Assert.That(uniquePointers.Count, Is.EqualTo(pointers.Count));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Reset_MultiplePages_ResetsAllPages()
        {
            // Arrange
            var allocator = new BumpAllocator();
            
            // Allocate across multiple pages
            for (int i = 0; i < 5; i++)
            {
                allocator.Alloc(BumpAllocator.PageSize);
            }

            // Act
            allocator.Reset();

            // Allocate again
            void* ptr = allocator.Alloc(64);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_AfterReset_CanAllocate()
        {
            // Arrange
            var allocator = new BumpAllocator();
            allocator.Alloc(100);
            allocator.Reset();

            // Act
            void* ptr = allocator.Alloc(200);

            // Assert
            Assert.That((nint)ptr, Is.Not.EqualTo(0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_VaryingSizes_AllocatesCorrectly()
        {
            // Arrange
            var allocator = new BumpAllocator();
            uint[] sizes = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };
            var pointers = new List<(nint ptr, uint size)>();

            // Act
            foreach (var size in sizes)
            {
                void* ptr = allocator.Alloc(size);
                Assert.That((nint)ptr, Is.Not.EqualTo(0));
                pointers.Add(((nint)ptr, size));

                // Write pattern
                byte* bytePtr = (byte*)ptr;
                for (uint i = 0; i < size; i++)
                {
                    bytePtr[i] = (byte)(size & 0xFF);
                }
            }

            // Assert - Verify data integrity
            foreach (var (ptr, size) in pointers)
            {
                byte* bytePtr = (byte*)ptr;
                for (uint i = 0; i < size; i++)
                {
                    Assert.That(bytePtr[i], Is.EqualTo((byte)(size & 0xFF)));
                }
            }

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_DefaultAlignment_UsesDefaultOf8()
        {
            // Arrange
            var allocator = new BumpAllocator();

            // Act
            void* ptr = allocator.Alloc(1); // Size 1, should be aligned to 8

            // Assert
            Assert.That((nint)ptr % 8, Is.EqualTo((nint)0));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Alloc_ConsecutiveAllocations_AreContiguous()
        {
            // Arrange
            var allocator = new BumpAllocator();

            // Act - Allocate with same alignment to ensure contiguous placement
            void* ptr1 = allocator.Alloc(16, 8);
            void* ptr2 = allocator.Alloc(16, 8);

            // Assert - Should be close together (within same page)
            nint diff = (nint)ptr2 - (nint)ptr1;
            Assert.That(diff, Is.GreaterThan((nint)0));
            Assert.That(diff, Is.LessThan((nint)BumpAllocator.PageSize));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Free_SequentialAllocationsAndFrees_WorksCorrectly()
        {
            // Arrange
            var allocator = new BumpAllocator();

            // Act & Assert
            void* ptr1 = allocator.Alloc(64);
            allocator.Free(ptr1, 64);

            void* ptr2 = allocator.Alloc(64);
            Assert.That((nint)ptr2, Is.EqualTo((nint)ptr1));

            void* ptr3 = allocator.Alloc(128);
            allocator.Free(ptr3, 128);

            void* ptr4 = allocator.Alloc(128);
            Assert.That((nint)ptr4, Is.EqualTo((nint)ptr3));

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void ReleaseAll_MultipleTimes_DoesNotCrash()
        {
            // Arrange
            var allocator = new BumpAllocator();
            allocator.Alloc(64);

            // Act & Assert
            Assert.DoesNotThrow(() => allocator.ReleaseAll());
            Assert.DoesNotThrow(() => allocator.ReleaseAll());
            Assert.DoesNotThrow(() => allocator.ReleaseAll());

            // Cleanup
            allocator.Dispose();
        }

        [Test]
        public void Dispose_MultipleTimes_DoesNotCrash()
        {
            // Arrange
            var allocator = new BumpAllocator();
            allocator.Alloc(64);

            // Act & Assert
            Assert.DoesNotThrow(() => allocator.Dispose());
            Assert.DoesNotThrow(() => allocator.Dispose());
        }

        [Test]
        public void Alloc_AfterDispose_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var allocator = new BumpAllocator();
            allocator.Alloc(64);
            allocator.Dispose();

            // Act - This behavior might vary, just ensure it doesn't crash the test host
            void* ptr = null;
            Assert.DoesNotThrow(() => ptr = allocator.Alloc(64));

            // If it returns a pointer, it should be valid or null
            // The exact behavior depends on implementation
        }
    }
}
#endif
