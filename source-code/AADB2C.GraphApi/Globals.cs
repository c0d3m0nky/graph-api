using System;

namespace AADB2C.GraphApi {
    public static class C
    {
        public static readonly string NL = Environment.NewLine;
        public static readonly ConsoleColor BaseForegroundColor = Console.ForegroundColor;
        public static readonly bool IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        public static readonly bool IsMac = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
        public static readonly string HiddenFilePrefix = IsWindows ? "" : ".";
    }
}