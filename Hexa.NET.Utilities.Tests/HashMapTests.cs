namespace Hexa.NET.Utilities.Tests
{
    [TestFixture]
    public unsafe class HashMapTests
    {
        [Test]
        public void AddAndContainsWorks()
        {
            HashMap<int, int> map = new();
            map.EnsureCapacity(8);
            map.Add(1, 100);
            map.Add(2, 200);
            Assert.That(map.Contains(1), Is.True);
            Assert.That(map.Contains(2), Is.True);
            Assert.That(map.Contains(3), Is.False);
            map.Release();
        }

        [Test]
        public void TryGetValueReturnsCorrectValue()
        {
            HashMap<int, int> map = new();
            map.EnsureCapacity(8);
            map.Add(42, 420);
            map.Add(7, 70);
            Assert.That(map.TryGetValue(42, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo(420));
            Assert.That(map.TryGetValue(7, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo(70));
            Assert.That(map.TryGetValue(99, out var v3), Is.False);
            map.Release();
        }

        [Test]
        public void AddDuplicateKeyThrows()
        {
            HashMap<int, int> map = new();
            map.EnsureCapacity(8);
            map.Add(5, 50);
            Assert.That(() => map.Add(5, 55), Throws.TypeOf<ArgumentException>());
            map.Release();
        }

        [Test]
        public void ResizeWorksCorrectly()
        {
            HashMap<int, int> map = new();
            map.EnsureCapacity(2);
            for (int i = 0; i < 100; i++)
            {
                map.Add(i, i * 10);
            }
            for (int i = 0; i < 100; i++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(map.Contains(i), Is.True);
                    Assert.That(map.TryGetValue(i, out var v), Is.True);
                    Assert.That(v, Is.EqualTo(i * 10));
                });
            }
            map.Release();
        }

        [Test]
        public void RemoveWorksCorrectly()
        {
            HashMap<int, int> map = new();
            map.EnsureCapacity(8);
            map.Add(1, 100);
            map.Add(2, 200);
            Assert.That(map.Contains(1), Is.True);
            Assert.That(map.Contains(2), Is.True);
            Assert.That(map.Remove(1), Is.True);
            Assert.That(map.Contains(1), Is.False);
            Assert.That(map.Remove(1), Is.False);
            Assert.That(map.Contains(2), Is.True);
            Assert.That(map.Remove(2), Is.True);
            Assert.That(map.Contains(2), Is.False);
            map.Release();
        }
    }
}