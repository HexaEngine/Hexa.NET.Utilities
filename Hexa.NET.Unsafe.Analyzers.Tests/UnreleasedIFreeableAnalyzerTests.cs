namespace Hexa.NET.Unsafe.Analyzers.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Testing;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<UnreleasedIFreeableAnalyzer>;

    [TestFixture]
    public class UnreleasedIFreeableAnalyzerTests
    {
        private static readonly string mock = @"

namespace Hexa.NET.Utilities
{
    public static unsafe class Utils { }

    public interface IFreeable
    {
        void Release();
    }

    public class FreeableResource : IFreeable
    {
        public void Release() { }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SuppressFreeWarningAttribute : System.Attribute
    {
    }
}
";

        private static DiagnosticResult ExpectedWarning(string memberName) =>
            new DiagnosticResult("UF001", DiagnosticSeverity.Warning)
                .WithMessage($"The field or property '{memberName}' implements IFreeable but is not released via Release()");

        [Test]
        public async Task TestFieldNotReleased_ShouldWarn()
        {
            var testCode = @"
class TestClass
{
    private FreeableResource _resource;

    public void UseResource()
    {
        // Some usage, but no Release call
    }
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("_resource").WithSpan(10, 5, 10, 40));
        }

        [Test]
        public async Task TestFieldReleased_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    private FreeableResource _resource;

    public void UseResource()
    {
        _resource.Release();
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestSuppressionAttributeOnField_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    [Hexa.NET.Utilities.SuppressFreeWarning]
    private FreeableResource _resource;

    public void UseResource()
    {
        // Suppression attribute applied, no Release call needed
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestSuppressionAttributeOnProperty_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    [Hexa.NET.Utilities.SuppressFreeWarning]
    public FreeableResource Resource { get; set; }

    public void UseResource()
    {
        // Suppression attribute applied, no Release call needed
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestPropertyNotReleased_ShouldWarn()
        {
            var testCode = @"
class TestClass
{
    public FreeableResource Resource { get; set; }

    public void UseResource()
    {
        // Some usage, but no Release call
    }
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("Resource").WithSpan(10, 5, 10, 51));
        }

        [Test]
        public async Task TestPropertyReleased_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    public FreeableResource Resource { get; set; }

    public void UseResource()
    {
        Resource.Release();
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            source = $@"
namespace TestNamespace
{{
    using static Hexa.NET.Utilities.Utils;
    using Hexa.NET.Utilities;

    {source}
}}
";

            source += mock;
            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }
    }
}