using System.Reflection;

namespace Hexa.NET.Utilities.Tests
{
    [SetUpFixture]
    public class TestBootstrap
    {
        [OneTimeSetUp]
        public void RunFirst()
        {
            Console.WriteLine($"CurrentDirectory: {Environment.CurrentDirectory}");
            Console.WriteLine($"BaseDirectory:    {AppContext.BaseDirectory}");
            Console.WriteLine($"Test Assembly:    {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Process Path:     {Environment.ProcessPath}");
        }
    }
}