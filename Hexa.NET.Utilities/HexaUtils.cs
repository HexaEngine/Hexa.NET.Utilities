using HexaGen.Runtime;
using System.Diagnostics;

namespace Hexa.NET.Utilities.Native
{
    public static class HexaUtilsConfig
    {
        public static bool AotStaticLink;
    }

    public static partial class HexaUtils
    {
        static HexaUtils()
        {
            if (HexaUtilsConfig.AotStaticLink)
            {
                InitApi(new NativeLibraryContext(Process.GetCurrentProcess().MainModule!.BaseAddress));
            }
            else
            {
                InitApi(new NativeLibraryContext(LibraryLoader.LoadLibrary(GetLibraryName, null)));
            }
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