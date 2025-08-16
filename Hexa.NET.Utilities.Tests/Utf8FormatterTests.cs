using System.Globalization;
using System.Text;
using Hexa.NET.Utilities.Text;

namespace Hexa.NET.Utilities.Tests;

public class Utf8FormatterTests
{
    [TestCase(1f, 0, "1")]
    [TestCase(10f, 0, "10")]
    [TestCase(0.5f, 1, "0.5")]
    [TestCase(0.75f, 2, "0.75")]
    [TestCase(0.125f, 3, "0.125")]
    [TestCase(0.0625f, 4, "0.0625")]
    [TestCase(0.03125f, 5, "0.03125")]
    [TestCase(0.015625f, 6, "0.015625")]
    [TestCase(0.0078125f, 7, "0.0078125")]
    public unsafe void FormatFloatTest(float value, int digit, string expected)
    {
        // Arrange
        byte* buffer = stackalloc byte[128];

        // Act
        int len = Utf8Formatter.Format(value, buffer, 128, CultureInfo.InvariantCulture, digit);
        ReadOnlySpan<byte> utf8Span = new ReadOnlySpan<byte>(buffer, len);

        // Assert
        Assert.That(Encoding.UTF8.GetString(utf8Span), Is.EqualTo(expected));
    }

    [TestCase(1, 0, "1")]
    [TestCase(10, 0, "10")]
    [TestCase(0.5, 1, "0.5")]
    [TestCase(0.75, 2, "0.75")]
    [TestCase(0.125, 3, "0.125")]
    [TestCase(0.0625, 4, "0.0625")]
    [TestCase(0.03125, 5, "0.03125")]
    [TestCase(0.015625, 6, "0.015625")]
    [TestCase(0.0078125, 7, "0.0078125")]
    public unsafe void FormatDoubleTest(double value, int digit, string expected)
    {
        // Arrange
        byte* buffer = stackalloc byte[128];

        // Act
        int len = Utf8Formatter.Format(value, buffer, 128, CultureInfo.InvariantCulture, digit);
        ReadOnlySpan<byte> utf8Span = new ReadOnlySpan<byte>(buffer, len);

        // Assert
        Assert.That(Encoding.UTF8.GetString(utf8Span), Is.EqualTo(expected));
    }
}