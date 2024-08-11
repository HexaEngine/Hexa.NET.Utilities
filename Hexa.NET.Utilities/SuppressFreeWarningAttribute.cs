namespace Hexa.NET.Utilities
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SuppressFreeWarningAttribute : Attribute
    {
    }
}