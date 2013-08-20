using System;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RTL_USER_PROCESS_PARAMETERS
    {
        public uint MaximumLength;
        public uint Length;
        public uint Flags;
        public uint DebugFlags;
        public IntPtr ConsoleHandle;
        public uint ConsoleFlags;
        public IntPtr StdInputHandle;
        public IntPtr StdOutputHandle;
        public IntPtr StdErrorHandle;
        public UNICODE_STRING CurrentDirectoryPath;
        public IntPtr CurrentDirectoryHandle;
        public UNICODE_STRING DllPath;
        public UNICODE_STRING ImagePathName;
        public UNICODE_STRING CommandLine;
    }
}
