namespace Hexa.NET.Utilities.Tests
{
    [TestFixture]
    public class UnsafeHashSetTests
    {
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
}