namespace Hexa.NET.Utilities.Tests
{
    using System.Diagnostics;

    [TestFixture]
    public class UnsafeDictionaryTests
    {
        [Test]
        public void TestEnumerator()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 10;
            dict[2] = 20;
            dict[3] = 30;

            IEnumerator<KeyValuePair<uint, int>> enumerator = dict.GetEnumerator();

            // Act
            var result = new List<KeyValuePair<uint, int>>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            // Assert
            var expected = new List<KeyValuePair<uint, int>>
            {
                new KeyValuePair<uint, int>(1, 10),
                new KeyValuePair<uint, int>(2, 20),
                new KeyValuePair<uint, int>(3, 30)
            };

            Assert.That(result, Is.EquivalentTo(expected));

            dict.Release();
        }

        [Test]
        public void TestKeyValueEnumerator()
        {
            // Arrange
            UnsafeDictionary<uint, int> dict = new UnsafeDictionary<uint, int>();

            // Populate the dictionary with data
            dict[1] = 10;
            dict[2] = 20;
            dict[3] = 30;

            // Act & Assert for Key Enumerator
            IEnumerator<uint> keyEnumerator = dict.Keys.GetEnumerator();
            var keyResult = new List<uint>();
            while (keyEnumerator.MoveNext())
            {
                keyResult.Add(keyEnumerator.Current);
            }

            var expectedKeys = new List<uint> { 1, 2, 3 };
            Assert.That(keyResult, Is.EquivalentTo(expectedKeys));

            // Act & Assert for Value Enumerator
            IEnumerator<int> valueEnumerator = dict.Values.GetEnumerator();
            var valueResult = new List<int>();
            while (valueEnumerator.MoveNext())
            {
                valueResult.Add(valueEnumerator.Current);
            }

            var expectedValues = new List<int> { 10, 20, 30 };
            Assert.That(valueResult, Is.EquivalentTo(expectedValues));

            // Clean up
            dict.Release();
        }

        [Test]
        public void TestInitialAdd()
        {
            UnsafeDictionary<uint, int> dict = default;

            dict.Clear();
            dict[1] = 2;
            Assert.That(dict[1], Is.EqualTo(2));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }

            dict.Release();
        }

        [Test]
        public void TestAddNewKey()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 2;
            dict[2] = 3;

            Assert.That(dict[1], Is.EqualTo(2));
            Assert.That(dict[2], Is.EqualTo(3));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
            dict.Release();
        }

        [Test]
        public void TestUpdateExistingKey()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 2;
            dict[1] = 4;

            Assert.That(dict[1], Is.EqualTo(4));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
            dict.Release();
        }

        [Test]
        public void TestRemoveKey()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 2;
            dict[2] = 3;

            dict.Remove(1);

            Assert.That(dict.ContainsKey(1), Is.False);
            Assert.That(dict[2], Is.EqualTo(3));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
            dict.Release();
        }

        [Test]
        public void TestRemoveNonExistentKey()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 2;

            Assert.DoesNotThrow(() => dict.Remove(2));

            Assert.That(dict[1], Is.EqualTo(2));
            Assert.That(dict.ContainsKey(2), Is.False);

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
            dict.Release();
        }

        [Test]
        public void TestEmptyDictionary()
        {
            UnsafeDictionary<uint, int> dict = default;
            Assert.That(dict, Is.Empty);

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }
            dict.Release();
        }

        [Test]
        public void TestReleaseResources()
        {
            UnsafeDictionary<uint, int> dict = default;
            dict[1] = 2;
            dict.Release();

            // Verify that capacity and size are reset to zero
            Assert.That(dict.Capacity, Is.EqualTo(0));
            Assert.That(dict.Size, Is.EqualTo(0));

            // Access the dictionary after release, expecting it to be reallocated
            dict[2] = 3;

            // Since dictionary is reallocated, previous values should not exist
            Assert.That(dict.ContainsKey(1), Is.False);
            Assert.That(dict[2], Is.EqualTo(3));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }

            // Verify that the dictionary's capacity and size have been updated correctly
            Assert.That(dict.Capacity, Is.EqualTo(3));
            Assert.That(dict.Size, Is.EqualTo(1));

            // Release the dictionary again
            dict.Release();

            // Verify that capacity and size are reset to zero again
            Assert.That(dict.Capacity, Is.EqualTo(0));
            Assert.That(dict.Size, Is.EqualTo(0));
        }

        [Test]
        public void StressTest()
        {
            const int iterations = 100000;
            UnsafeDictionary<uint, int> dict = default;
            Random random = new Random();
            Stopwatch stopwatch = new Stopwatch();

            const int range = int.MaxValue / iterations;

            List<(uint, int)> keyValues = new();

            StressInsert(iterations, random, range, keyValues, ref dict); // warmup

            keyValues.Clear();
            dict.Clear();

            stopwatch.Start();
            StressInsert(iterations, random, range, keyValues, ref dict);
            stopwatch.Stop();

            Console.WriteLine($"Insert stress test completed in {stopwatch.Elapsed.TotalMilliseconds} ms");

            StressLookup(keyValues, ref dict); // warmup

            // Verify that the dictionary contains the expected values
            stopwatch.Restart();
            StressLookup(keyValues, ref dict);
            stopwatch.Stop();

            Console.WriteLine($"Lookup stress test completed in {stopwatch.Elapsed.TotalMilliseconds} ms");

            var clone = dict.Clone();
            StressRemove(keyValues, ref clone);

            Assert.That(clone.Size, Is.EqualTo(0));
            clone.Release();

            stopwatch.Restart();
            StressRemove(keyValues, ref dict);
            stopwatch.Stop();

            Console.WriteLine($"Delete stress test completed in {stopwatch.Elapsed.TotalMilliseconds} ms");

            Assert.That(dict.Size, Is.EqualTo(0));

            dict.Release();

            Assert.Pass();
        }

        private void StressRemove(List<(uint, int)> keyValues, ref UnsafeDictionary<uint, int> dict)
        {
            for (int i = 0; i < keyValues.Count; i++)
            {
                var (key, _) = keyValues[i];
                dict.Remove(key);
            }
            int siu = dict.Size;
        }

        private void StressLookup(List<(uint, int)> keyValues, ref UnsafeDictionary<uint, int> dict)
        {
            for (int i = 0; i < keyValues.Count; i++)
            {
                var (key, value) = keyValues[i];
                if (dict[key] != value)
                {
                    Assert.Fail();
                }
            }
        }

        private void StressInsert(int iterations, Random random, int range, List<(uint, int)> keyValues, ref UnsafeDictionary<uint, int> dict)
        {
            for (int i = 0; i < iterations; i++)
            {
                uint key = (uint)random.Next(range * i, range * (i + 1));

                int value = random.Next();
                dict[key] = value;
                keyValues.Add((key, value));
            }
        }
    }
}