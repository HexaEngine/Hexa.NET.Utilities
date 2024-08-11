namespace Hexa.NET.Unsafe.Analyzers.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Testing;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<ReadonlyFreeableStructAnalyzer>;

    [TestFixture]
    public class ReadonlyFreeableStructAnalyzerTests
    {
        private static readonly string mock = @"
namespace Hexa.NET.Utilities
{
    public static class Utils { }

    public interface IFreeable
    {
        void Release();
    }

    public struct FreeableStruct : IFreeable
    {
        public void Release() { }
    }

    public struct NonFreeableStruct
    {
        public int Value;
    }
}
";

        private static DiagnosticResult ExpectedWarning(string structName) =>
            new DiagnosticResult("RFS001", DiagnosticSeverity.Warning)
                .WithMessage($"The struct '{structName}' implements IFreeable and should not be used with the readonly modifier");

        [Test]
        public async Task TestReadonlyFreeableStruct_ShouldWarn()
        {
            var testCode = @"
class TestClass
{
    private readonly FreeableStruct _freeable;
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("FreeableStruct").WithSpan(10, 37, 10, 46));
        }

        [Test]
        public async Task TestNonReadonlyFreeableStruct_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    private FreeableStruct _freeable;
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestReadonlyNonFreeableStruct_ShouldNotWarn()
        {
            var testCode = @"
class TestClass
{
    private readonly NonFreeableStruct _nonFreeable;
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

            source += mock; // appending makes analysis more stable
            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }
    }
}