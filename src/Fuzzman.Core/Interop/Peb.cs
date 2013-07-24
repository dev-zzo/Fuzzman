using System;
using System.Runtime.InteropServices;

// http://undocumented.ntinternals.net/UserMode/Undocumented%20Functions/NT%20Objects/Process/PEB.html

namespace Fuzzman.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PEB
    {
        public byte InheritedAddressSpace;
        public byte ReadImageFileExecOptions;
        public byte BeingDebugged;
        public byte Spare;
        public IntPtr Mutant;
        public IntPtr ImageBaseAddress;
        public IntPtr LoaderData; // PPEB_LDR_DATA
        public IntPtr ProcessParameters; // PRTL_USER_PROCESS_PARAMETERS
        public IntPtr SubSystemData;
        public IntPtr ProcessHeap;
        public IntPtr FastPebLock;
        public IntPtr FastPebLockRoutine; // PPEBLOCKROUTINE
        public IntPtr FastPebUnlockRoutine; // PPEBLOCKROUTINE
        public uint EnvironmentUpdateCount;
        public IntPtr KernelCallbackTable;
        public IntPtr EventLogSection;
        public IntPtr EventLog;
        public IntPtr FreeList; // PPEB_FREE_BLOCK
        public uint TlsExpansionCounter;
        public IntPtr TlsBitmap;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] TlsBitmapBits;
        public IntPtr ReadOnlySharedMemoryBase;
        public IntPtr ReadOnlySharedMemoryHeap;
        public IntPtr ReadOnlyStaticServerData;
        public IntPtr AnsiCodePageData;
        public IntPtr OemCodePageData;
        public IntPtr UnicodeCaseTableData;
        public uint NumberOfProcessors;
        public uint NtGlobalFlag;
        public uint Spare2;
        public UInt64 CriticalSectionTimeout;
        public uint HeapSegmentReserve;
        public uint HeapSegmentCommit;
        public uint HeapDeCommitTotalFreeThreshold;
        public uint HeapDeCommitFreeBlockThreshold;
        public uint NumberOfHeaps;
        public uint MaximumNumberOfHeaps;
        public IntPtr ProcessHeaps;
        public IntPtr GdiSharedIntPtrTable;
        public IntPtr ProcessStarterHelper;
        public IntPtr GdiDCAttributeList;
        public IntPtr LoaderLock;
        public uint OSMajorVersion;
        public uint OSMinorVersion;
        public uint OSBuildNumber;
        public uint OSPlatformId;
        public uint ImageSubSystem;
        public uint ImageSubSystemMajorVersion;
        public uint ImageSubSystemMinorVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x22)]
        public uint[] GdiIntPtrBuffer;
        public uint PostProcessInitRoutine;
        public uint TlsExpansionBitmap;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        public byte[] TlsExpansionBitmapBits;
        public uint SessionId;
    }
}
