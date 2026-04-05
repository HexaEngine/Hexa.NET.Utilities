namespace Hexa.NET.Utilities.Native
{
    public static partial class HexaUtils
    {
        static HexaUtils()
        {
            InitApi();
        }

        public static string GetLibraryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "HexaUtils";
            }

            return "libHexaUtils";
        }
    }
}