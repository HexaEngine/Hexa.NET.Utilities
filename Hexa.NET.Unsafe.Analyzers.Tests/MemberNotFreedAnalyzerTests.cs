namespace Hexa.NET.Unsafe.Analyzers.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Testing;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<MemberNotFreedAnalyzer>;

    [TestFixture]
    public class MemberNotFreedAnalyzerTests
    {
        private static readonly string mock = @"
namespace Hexa.NET.Utilities
{
    using System;

    public static unsafe class Utils
    {
        public static unsafe void* Alloc() => null;
        public static unsafe T* AllocT<T>() where T : unmanaged
        {
            return null;
        }
        public static unsafe void Free(void* ptr) { }
    }

     [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SuppressFreeWarningAttribute : System.Attribute { }
}
";

        private static DiagnosticResult ExpectedWarning(string memberName) =>
            new DiagnosticResult("MNF001", DiagnosticSeverity.Warning)
                .WithMessage($"The member '{memberName}' is not freed in any method of the containing class/struct");

        [Test]
        public async Task TestMemberNotFreed_ShouldWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    private int* _ptr;

    public unsafe void Allocate()
    {
        _ptr = AllocT<int>();
    }
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("_ptr").WithSpan(10, 5, 10, 23));
        }

        [Test]
        public async Task TestMemberFreed_ShouldNotWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    private int* _ptr;

    public unsafe void Allocate()
    {
        _ptr = AllocT<int>();
    }

    public unsafe void Deallocate()
    {
        Free(_ptr);
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestSuppressionAttributeOnField_ShouldNotWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    [SuppressFreeWarning]
    private int* _ptr;

    public unsafe void Allocate()
    {
        _ptr = AllocT<int>();
    }
}";

            await VerifyAnalyzerAsync(testCode);
        }

        [Test]
        public async Task TestMultipleFieldsOneNotFreed_ShouldWarn()
        {
            var testCode = @"
unsafe class TestClass
{
    private int* _ptr1;
    private float* _ptr2;

    public unsafe void Allocate()
    {
        _ptr1 = AllocT<int>();
        _ptr2 = AllocT<float>();
    }

    public unsafe void Deallocate()
    {
        Free(_ptr1);
    }
}";

            await VerifyAnalyzerAsync(testCode, ExpectedWarning("_ptr2").WithSpan(11, 5, 11, 26));
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