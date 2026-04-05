using System.Reflection;
using NUnit.Framework;

namespace Hexa.NET.Utilities.Tests
{
    [TestFixture]
    public class ATestEnv
    {
        [Test]
        public void DumpPaths()
        {
            TestContext.Progress.WriteLine(
                $"CurrentDirectory: {Environment.CurrentDirectory}\n" +
                $"BaseDirectory:    {AppContext.BaseDirectory}\n" +
                $"Test Assembly:    {Assembly.GetExecutingAssembly().Location}\n" +
                $"Process Path:     {Environment.ProcessPath}");
        }
    }
}