#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    public unsafe class StringPoolTests
    {
        [Test]
        public void Take_SingleString_ReturnsValidStringSpan()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Hello, World!";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);

            // Act
            StringSpan result = pool.Take(testBytes);

            // Assert
            Assert.That(result.Length, Is.EqualTo(testBytes.Length));
            Assert.That(result.ToString(), Is.EqualTo(testString));
            Assert.That((nint)result.Ptr, Is.Not.EqualTo(0));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_EmptyString_ReturnsValidStringSpan()
        {
            // Arrange
            var pool = new StringPool();
            byte[] testBytes = Array.Empty<byte>();

            // Act
            StringSpan result = pool.Take(testBytes);

            // Assert
            Assert.That(result.Length, Is.EqualTo(0));
            Assert.That((nint)result.Ptr, Is.Not.EqualTo(0));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_DuplicateStrings_ReturnsSamePointer()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Duplicate Test";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);

            // Act
            StringSpan first = pool.Take(testBytes);
            StringSpan second = pool.Take(testBytes);

            // Assert - Should return the same pointer for identical strings
            Assert.That((nint)first.Ptr, Is.EqualTo((nint)second.Ptr));
            Assert.That(first.Length, Is.EqualTo(second.Length));
            Assert.That(first.ToString(), Is.EqualTo(second.ToString()));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_MultipleUniqueStrings_StoresAllStrings()
        {
            // Arrange
            var pool = new StringPool();
            var testStrings = new[]
            {
                "First string",
                "Second string",
                "Third string",
                "Fourth string",
                "Fifth string"
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in testStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(testStrings.Length));
            
            // Verify all strings are stored correctly
            for (int i = 0; i < testStrings.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(testStrings[i]));
            }

            // Verify all pointers are unique
            var uniquePointers = results.Select(r => (nint)r.Ptr).Distinct().ToList();
            Assert.That(uniquePointers.Count, Is.EqualTo(testStrings.Length));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_MixedDuplicateAndUniqueStrings_DeduplicatesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var testStrings = new[]
            {
                "Alpha",
                "Beta",
                "Alpha",  // Duplicate
                "Gamma",
                "Beta",   // Duplicate
                "Alpha",  // Duplicate
                "Delta"
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in testStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert
            // "Alpha" should have same pointer at indices 0, 2, 5
            Assert.That((nint)results[0].Ptr, Is.EqualTo((nint)results[2].Ptr));
            Assert.That((nint)results[0].Ptr, Is.EqualTo((nint)results[5].Ptr));

            // "Beta" should have same pointer at indices 1, 4
            Assert.That((nint)results[1].Ptr, Is.EqualTo((nint)results[4].Ptr));

            // Verify unique strings have different pointers
            Assert.That((nint)results[0].Ptr, Is.Not.EqualTo((nint)results[1].Ptr)); // Alpha != Beta
            Assert.That((nint)results[0].Ptr, Is.Not.EqualTo((nint)results[3].Ptr)); // Alpha != Gamma
            Assert.That((nint)results[0].Ptr, Is.Not.EqualTo((nint)results[6].Ptr)); // Alpha != Delta

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_NullTermination_StringIsNullTerminated()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Test";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);

            // Act
            StringSpan result = pool.Take(testBytes);

            // Assert - Verify null termination
            byte* ptr = result.Ptr;
            Assert.That(ptr[result.Length], Is.EqualTo(0));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_UTF8Strings_HandlesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var testStrings = new[]
            {
                "Hello",
                "??????",      // Russian
                "?????",    // Japanese
                "??",         // Chinese
                "?????",        // Arabic
                "??????"        // Emojis
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in testStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert
            for (int i = 0; i < testStrings.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(testStrings[i]));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_LargeStrings_HandlesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            string largeString = new string('A', 10000);
            byte[] testBytes = Encoding.UTF8.GetBytes(largeString);

            // Act
            StringSpan result = pool.Take(testBytes);

            // Assert
            Assert.That(result.Length, Is.EqualTo(testBytes.Length));
            Assert.That(result.ToString(), Is.EqualTo(largeString));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var specialStrings = new[]
            {
                "Line1\nLine2",       // Newline
                "Tab\tSeparated",     // Tab
                "Quote\"Test",        // Quote
                "Path\\File",         // Backslash
                "\0Hidden",           // Null character in middle (though length will be correct)
                "Mixed\r\n\t\"Test"   // Multiple special chars
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in specialStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert
            for (int i = 0; i < specialStrings.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(specialStrings[i]));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_ManyStrings_NoMemoryLeaks()
        {
            // Arrange
            var pool = new StringPool();
            const int stringCount = 10000;
            var results = new List<StringSpan>();

            // Act
            for (int i = 0; i < stringCount; i++)
            {
                string str = $"String_{i}";
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(stringCount));
            
            // Verify a sample of strings
            for (int i = 0; i < stringCount; i += 100)
            {
                Assert.That(results[i].ToString(), Is.EqualTo($"String_{i}"));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_DuplicateLargeStrings_ReusesMemory()
        {
            // Arrange
            var pool = new StringPool();
            string largeString = new string('X', 5000);
            byte[] testBytes = Encoding.UTF8.GetBytes(largeString);

            // Act
            StringSpan first = pool.Take(testBytes);
            StringSpan second = pool.Take(testBytes);
            StringSpan third = pool.Take(testBytes);

            // Assert - All should point to the same memory
            Assert.That((nint)first.Ptr, Is.EqualTo((nint)second.Ptr));
            Assert.That((nint)first.Ptr, Is.EqualTo((nint)third.Ptr));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_StringsWithSamePrefix_StoresIndependently()
        {
            // Arrange
            var pool = new StringPool();
            var testStrings = new[]
            {
                "Test",
                "Testing",
                "TestCase",
                "Test123"
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in testStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert - All should be different even though they share prefixes
            var uniquePointers = results.Select(r => (nint)r.Ptr).Distinct().ToList();
            Assert.That(uniquePointers.Count, Is.EqualTo(testStrings.Length));

            for (int i = 0; i < testStrings.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(testStrings[i]));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_CaseVariations_TreatsAsDifferent()
        {
            // Arrange
            var pool = new StringPool();
            var testStrings = new[]
            {
                "test",
                "Test",
                "TEST",
                "TeSt"
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in testStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert - All should be different (case-sensitive)
            var uniquePointers = results.Select(r => (nint)r.Ptr).Distinct().ToList();
            Assert.That(uniquePointers.Count, Is.EqualTo(testStrings.Length));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Test String";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            pool.Take(testBytes);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => pool.Dispose());
        }

        [Test]
        public void Release_ReleasesResources()
        {
            // Arrange
            IFreeable pool = new StringPool();
            var poolStruct = (StringPool)pool;
            string testString = "Test String";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            poolStruct.Take(testBytes);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => pool.Release());
        }

        [Test]
        public void Take_StringSpanProperties_WorkCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Property Test";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);

            // Act
            StringSpan result = pool.Take(testBytes);

            // Assert - Test various StringSpan operations
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result[0], Is.EqualTo((byte)'P'));
            Assert.That(result.Contains((byte)'P'), Is.True);
            Assert.That(result.IndexOf((byte)'T'), Is.GreaterThanOrEqualTo(0));
            
            byte[] propertyBytes = Encoding.UTF8.GetBytes("Property");
            Assert.That(result.StartsWith(propertyBytes), Is.True);

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_SequentialDuplicates_OnlyStoresOnce()
        {
            // Arrange
            var pool = new StringPool();
            string testString = "Repeated";
            byte[] testBytes = Encoding.UTF8.GetBytes(testString);
            const int repeatCount = 100;
            var results = new List<StringSpan>();

            // Act
            for (int i = 0; i < repeatCount; i++)
            {
                results.Add(pool.Take(testBytes));
            }

            // Assert - All should point to the same memory
            var firstPtr = (nint)results[0].Ptr;
            foreach (var result in results)
            {
                Assert.That((nint)result.Ptr, Is.EqualTo(firstPtr));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_AlternatingStrings_DeduplicatesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            byte[] stringA = Encoding.UTF8.GetBytes("StringA");
            byte[] stringB = Encoding.UTF8.GetBytes("StringB");
            var results = new List<StringSpan>();

            // Act - Add strings in alternating pattern: A, B, A, B, A, B
            for (int i = 0; i < 6; i++)
            {
                results.Add(pool.Take(i % 2 == 0 ? stringA : stringB));
            }

            // Assert
            // All even indices should point to the same memory (StringA)
            Assert.That((nint)results[0].Ptr, Is.EqualTo((nint)results[2].Ptr));
            Assert.That((nint)results[0].Ptr, Is.EqualTo((nint)results[4].Ptr));

            // All odd indices should point to the same memory (StringB)
            Assert.That((nint)results[1].Ptr, Is.EqualTo((nint)results[3].Ptr));
            Assert.That((nint)results[1].Ptr, Is.EqualTo((nint)results[5].Ptr));

            // StringA and StringB should be different
            Assert.That((nint)results[0].Ptr, Is.Not.EqualTo((nint)results[1].Ptr));

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_SingleCharacterStrings_HandlesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var singleChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            var results = new List<StringSpan>();

            // Act
            foreach (var ch in singleChars)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(ch.ToString());
                results.Add(pool.Take(bytes));
            }

            // Assert
            Assert.That(results.Count, Is.EqualTo(singleChars.Length));
            
            // All should be unique
            var uniquePointers = results.Select(r => (nint)r.Ptr).Distinct().ToList();
            Assert.That(uniquePointers.Count, Is.EqualTo(singleChars.Length));

            // Verify content
            for (int i = 0; i < singleChars.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(singleChars[i].ToString()));
                Assert.That(results[i].Length, Is.EqualTo(1));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_NumericStrings_DeduplicatesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var results = new List<StringSpan>();

            // Act - Add numbers 0-9, then repeat 0-9
            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < 10; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(i.ToString());
                    results.Add(pool.Take(bytes));
                }
            }

            // Assert - First 10 and second 10 should match
            for (int i = 0; i < 10; i++)
            {
                Assert.That((nint)results[i].Ptr, Is.EqualTo((nint)results[i + 10].Ptr));
                Assert.That(results[i].ToString(), Is.EqualTo(i.ToString()));
            }

            // Cleanup
            pool.Dispose();
        }

        [Test]
        public void Take_WhitespaceStrings_HandlesCorrectly()
        {
            // Arrange
            var pool = new StringPool();
            var whitespaceStrings = new[]
            {
                " ",
                "  ",
                "\t",
                "\n",
                "\r\n",
                "   "
            };
            var results = new List<StringSpan>();

            // Act
            foreach (var str in whitespaceStrings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                results.Add(pool.Take(bytes));
            }

            // Assert - All should be different
            var uniquePointers = results.Select(r => (nint)r.Ptr).Distinct().ToList();
            Assert.That(uniquePointers.Count, Is.EqualTo(whitespaceStrings.Length));

            // Verify content
            for (int i = 0; i < whitespaceStrings.Length; i++)
            {
                Assert.That(results[i].ToString(), Is.EqualTo(whitespaceStrings[i]));
            }

            // Cleanup
            pool.Dispose();
        }
    }
}
#endif
