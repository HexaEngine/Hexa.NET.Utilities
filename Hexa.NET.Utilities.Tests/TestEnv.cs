using System.Reflection;

namespace Hexa.NET.Utilities.Tests
{
    [TestFixture]
    public class TestBootstrap
    {
        [Test]
        public void RunFirst()
        {
            Assert.Warn($"CurrentDirectory: {Environment.CurrentDirectory}");
            Assert.Warn($"BaseDirectory:    {AppContext.BaseDirectory}");
            Assert.Warn($"Test Assembly:    {Assembly.GetExecutingAssembly().Location}");
            Assert.Warn($"Process Path:     {Environment.ProcessPath}");
        }
    }
}