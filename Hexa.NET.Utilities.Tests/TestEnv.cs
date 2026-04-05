using System.Reflection;

namespace Hexa.NET.Utilities.Tests
{
    [TestFixture]
    public class ATestEnv
    {
        [Test]
        public void DumpPaths()
        {
            Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");
            Console.WriteLine($"BaseDirectory:    {AppContext.BaseDirectory}");
            Console.WriteLine($"Test Assembly:    {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Process Path:     {Environment.ProcessPath}");
        }
    }
}