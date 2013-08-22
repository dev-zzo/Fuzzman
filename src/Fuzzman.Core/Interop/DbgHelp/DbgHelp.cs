using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop.DbgHelp
{
    public enum IMAGE_FILE_MACHINE : uint
    {
        I386 = 0x014c,
        IA64 = 0x0200,
        AMD64 = 0x8664,
    }

    public enum ADDRESS_MODE : uint
    {
        AddrMode1616,
        AddrMode1632,
        AddrModeReal,
        AddrModeFlat,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ADDRESS64
    {
        public ulong Offset;
        public ushort Segment;
        public ADDRESS_MODE Mode;
    }

    public static class DbgHelp
    {
    }
}
