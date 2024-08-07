namespace Hexa.NET.Utilities.Tests
{
    using System.Diagnostics;

    [TestFixture]
    public class UnsafeDictionaryTests
    {
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

            Assert.IsFalse(dict.ContainsKey(1));
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
            Assert.IsFalse(dict.ContainsKey(2));

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
            Assert.IsEmpty(dict);

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
            Assert.IsFalse(dict.ContainsKey(1));
            Assert.That(dict[2], Is.EqualTo(3));

            foreach (var item in dict)
            {
                Console.WriteLine(item.Key + " " + item.Value);
            }

            // Verify that the dictionary's capacity and size have been updated correctly
            Assert.Greater(dict.Capacity, 0);
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