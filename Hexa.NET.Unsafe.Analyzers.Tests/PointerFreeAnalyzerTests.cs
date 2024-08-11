namespace Hexa.NET.Unsafe.Analyzers.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Testing;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<PointerFreeAnalyzer>;

    [TestFixture]
    public class PointerFreeAnalyzerTests
    {
        private static DiagnosticResult ExpectedWarning(string methodName) =>
            new DiagnosticResult("PFA001", DiagnosticSeverity.Warning)
                .WithMessage($"Memory allocated with {methodName} is not freed or written to a field");

        private static readonly string mock = @"
namespace Hexa.NET.Utilities
{
    public static unsafe class Utils
    {
        public static unsafe void* Alloc() => null;
        public static unsafe T* AllocT<T>() where T : unmanaged
        {
            return null;
        }
        public static unsafe void Free(void* ptr) { }
    }
}
";

        [Test]
        public async Task TestAllocWithoutFreeOrFieldAssignment_ShouldWarn()
        {
            string path = "../../../../Hexa.NET.Utilities/Utils.cs";

            path = Path.GetFullPath(path);

            var testCode = @"
unsafe class TestClass
{
    unsafe void TestMethod()
    {
        var ptr = Alloc();
    }
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("Alloc").WithSpan(12, 19, 12, 26));
        }

        [Test]
        public async Task TestAllocWithFree_ShouldNotWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    unsafe void TestMethod()
    {
        var ptr = Alloc();
        Free(ptr);
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestAllocWithFieldAssignment_ShouldNotWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    private void* _ptr;

    unsafe void TestMethod()
    {
        _ptr = Alloc();
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestAllocWithPropAssignment_ShouldNotWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    private void* _ptr { get; set; }

    unsafe void TestMethod()
    {
        _ptr = Alloc();
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

            source = source + mock;
            await VerifyCS.VerifyAnalyzerAsync(source, expected);
        }
    }
}