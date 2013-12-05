using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Fuzzman.Agent.Fuzzers.TTF
{
    [StructLayout(LayoutKind.Sequential)]
    struct TtfOffsetTable
    {
        public uint SfntVersion;
        public ushort NumTables;
        public ushort SearchRange;
        public ushort EntrySelector;
        public ushort RangeShift;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TtfTableDirectoryEntry
    {
        public uint Tag;
        public uint CheckSum;
        public uint Offset;
        public uint Length;
    }
}
