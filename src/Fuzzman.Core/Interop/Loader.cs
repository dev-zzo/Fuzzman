using System;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LDR_MODULE
    {
        public LIST_ENTRY InLoadOrderModuleList;
        public LIST_ENTRY InMemoryOrderModuleList;
        public LIST_ENTRY InInitializationOrderModuleList;
        public IntPtr BaseAddress;
        public IntPtr EntryPoint;
        public uint SizeOfImage;
        public UNICODE_STRING FullDllName;
        public UNICODE_STRING BaseDllName;
        public uint Flags;
        public ushort LoadCount;
        public ushort TlsIndex;
        public LIST_ENTRY HashTableEntry;
        public uint TimeDateStamp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PEB_LDR_DATA
    {
        public uint Length;
        public byte Initialized;
        public byte Unused1;
        public byte Unused2;
        public byte Unused3;
        public IntPtr SsHandle;
        public LIST_ENTRY InLoadOrderModuleList;
        public LIST_ENTRY InMemoryOrderModuleList;
        public LIST_ENTRY InInitializationOrderModuleList;
    }
}
