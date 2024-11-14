namespace Hexa.NET.Utilities.Tests
{
    using System.Diagnostics;

    [TestFixture]
    public class UnsafeHashSetTests
    {
        [Test]
        public void TestEnumerator()
        {
            UnsafeHashSet<int> dict = default;
            dict.Add(10);
            dict.Add(20);
            dict.Add(30);

            IEnumerator<int> enumerator = dict.GetEnumerator();

            // Act
            var result = new List<int>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            // Assert
            var expected = new List<int>
            {
                10,
                20,
                30
            };

            Assert.That(result, Is.EquivalentTo(expected));

            dict.Release();
        }

        [Test]
        public void TestAdd()
        {
            // Arrange
            UnsafeHashSet<int> set = default;

            // Act
            set.Add(1);

            // Assert
            Assert.That(set.Contains(1), Is.True);

            // Clean up
            set.Release();
        }

        [Test]
        public void TestRemove()
        {
            // Arrange
            UnsafeHashSet<int> set = default;
            set.Add(1);

            // Act
            set.Remove(1);

            // Assert
            Assert.That(set.Contains(1), Is.False);

            // Clean up
            set.Release();
        }

        [Test]
        public void TestReleaseResources()
        {
            // Arrange
            UnsafeHashSet<int> set = default;
            set.Add(1);

            // Act
            set.Release();
            set.Add(2); // This should resurrect the set
            bool resurrected = set.Contains(2);

            // Assert
            Assert.That(resurrected, Is.True, "The set should contain the item after resurrection.");

            // Clean up
            set.Release();

            Assert.Multiple(() =>
            {
                Assert.That(set.Capacity, Is.EqualTo(0));
                Assert.That(set.Size, Is.EqualTo(0));
            });
        }
    }

    [TestFixture]
    public unsafe class UnsafeListTests
    {
        [Test]
        public void Add_ShouldStoreValueAndResizeAutomatically()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization

            // Act
            list.Add(42);
            list.Add(24);
            list.Add(12);  // This should trigger a resize if needed

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(42));
            Assert.That(list[1], Is.EqualTo(24));
            Assert.That(list[2], Is.EqualTo(12));

            list.Release();
        }

        [Test]
        public void Indexer_ShouldReturnCorrectValue()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);

            // Act
            int value = list[0];

            // Assert
            Assert.That(value, Is.EqualTo(42));

            list.Release();
        }

        [Test]
        public void Count_ShouldReflectNumberOfElements()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization

            // Act
            list.Add(42);
            list.Add(24);

            // Assert
            Assert.That(list.Count, Is.EqualTo(2));

            list.Release();
        }

        [Test]
        public void Clear_ShouldRemoveAllElements()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);
            list.Add(24);

            // Act
            list.Clear();

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));

            list.Release();
        }

        [Test]
        public void Release_ShouldFreeMemory()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);

            // Act
            list.Release();

            // Assert
            Assert.That(list.Data == null);       // Data pointer should be null
            Assert.That(list.Capacity, Is.EqualTo(0)); // Capacity should be 0
            Assert.That(list.Count, Is.EqualTo(0));    // Count should be 0
        }

        [Test]
        public void AddingAfterRelease_ShouldNotCrashButDoNothing()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);
            list.Release();

            // Assert
            // Since the list was released, Data should still be null, and nothing should be added.
            Assert.That(list.Data == null);          // Data pointer should remain null
            Assert.That(list.Capacity, Is.EqualTo(0)); // Capacity should still be 0
            Assert.That(list.Count, Is.EqualTo(0));    // Count should still be 0

            // Act
            list.Add(24);

            Assert.That(list.Data != null);          // Data pointer should be reinitialized
            Assert.That(list.Capacity, Is.GreaterThan(0)); // Capacity should reflect the reinitialization
            Assert.That(list.Count, Is.EqualTo(1));       // Count should be 1 after adding the new item
            Assert.That(list[0], Is.EqualTo(24));         // The new item should be correctly stored

            list.Release();
        }

        [Test]
        public void List_ShouldHandleAutomaticResurrection()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Release();

            // Act
            list.Add(24);

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));    // Count should reflect the added item
            Assert.That(list[0], Is.EqualTo(24));      // The value should be stored correctly

            list.Release();
        }

        [Test]
        public void Contains_ShouldReturnTrueIfItemExists()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);

            // Act
            bool contains = list.Contains(42);

            // Assert
            Assert.That(contains, Is.True);

            list.Release();
        }

        [Test]
        public void IndexOf_ShouldReturnCorrectIndex()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);
            list.Add(24);

            // Act
            int index = list.IndexOf(24);

            // Assert
            Assert.That(index, Is.EqualTo(1));

            list.Release();
        }

        [Test]
        public void Remove_ShouldRemoveItemIfExists()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.Add(42);
            list.Add(24);

            // Act
            bool removed = list.Remove(42);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(24));

            list.Release();
        }

        [Test]
        public void PushBack_ShouldAddElementToTheEnd()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization

            // Act
            list.PushBack(42);
            list.PushBack(24);

            // Assert
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(42));
            Assert.That(list[1], Is.EqualTo(24));

            list.Release();
        }

        [Test]
        public void PopBack_ShouldRemoveLastElement()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization
            list.PushBack(42);
            list.PushBack(24);

            // Act
            list.PopBack();

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(42));

            list.Release();
        }

        [Test]
        public void StressTest()
        {
            // Arrange
            UnsafeList<int> list = default;  // Default initialization

            long begin = Stopwatch.GetTimestamp();

            const int N = 1000000;

            // Act
            for (int i = 0; i < N; i++)
            {
                list.Add(i);
            }

            long end = Stopwatch.GetTimestamp();

            double seconds = (end - begin) / (double)Stopwatch.Frequency;

            Console.WriteLine($"Iterations: {N}");
            Console.WriteLine($"Elapsed time: {seconds * 1000} ms");
            Console.WriteLine($"Per add: {seconds / N * 1000 * 1000 * 1000} ns");

            // Assert
            Assert.That(list.Count, Is.EqualTo(N));

            list.Release();
        }
    }
}