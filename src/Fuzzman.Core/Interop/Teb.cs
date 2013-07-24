using System;
using System.Runtime.InteropServices;

// http://undocumented.ntinternals.net/UserMode/Undocumented%20Functions/NT%20Objects/Thread/TEB.html

namespace Fuzzman.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CLIENT_ID
    {
        public IntPtr UniqueProcess;
        public IntPtr UniqueThread;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NT_TIB
    {
        public IntPtr ExceptionList;
        public IntPtr StackBase;
        public IntPtr StackLimit;
        public IntPtr SubSystemTib;
        public IntPtr FiberDataOrVersion;
        public IntPtr ArbitraryUserPointer;
        public IntPtr Self;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TEB
    {
        public NT_TIB Tib;
        public IntPtr EnvironmentPointer;
        public CLIENT_ID   Cid;
        public IntPtr ActiveRpcInfo;
        public IntPtr ThreadLocalStoragePointer;
        public IntPtr Peb;
        public uint LastErrorValue;
        public uint CountOfOwnedCriticalSections;
        public IntPtr CsrClientThread;
        public IntPtr Win32ThreadInfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1F)]
        public uint[] Win32ClientInfo;
        public IntPtr WOW32Reserved;
        public uint CurrentLocale;
        public uint FpSoftwareStatusRegister;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x36)]
        public IntPtr[] SystemReserved1;
        public IntPtr Spare1;
        public uint[] ExceptionCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x28)]
        public uint[] SpareBytes1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x0A)]
        public IntPtr[] SystemReserved2;
        public uint GdiRgn;
        public uint GdiPen;
        public uint GdiBrush;
        public CLIENT_ID RealClientId;
        public IntPtr GdiCachedProcessHandle;
        public uint GdiClientPID;
        public uint GdiClientTID;
        public IntPtr GdiThreadLocaleInfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x05)]
        public IntPtr[] UserReserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x118)]
        public IntPtr[] GlDispatchTable;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1A)]
        public uint[] GlReserved1;
        public IntPtr GlReserved2;
        public IntPtr GlSectionInfo;
        public IntPtr GlSection;
        public IntPtr GlTable;
        public IntPtr GlCurrentRC;
        public IntPtr GlContext;
        public NTSTATUS LastStatusValue;
        public UNICODE_STRING StaticUnicodeString;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x105)]
        public char[] StaticUnicodeBuffer;
        public IntPtr DeallocationStack;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        public IntPtr[] TlsSlots;
        public LIST_ENTRY TlsLinks;
        public IntPtr Vdm;
        public IntPtr ReservedForNtRpc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x2)]
        public IntPtr[] DbgSsReserved;
        public uint HardErrorDisabled;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public IntPtr[] Instrumentation;
        public IntPtr WinSockData;
        public uint GdiBatchCount;
        public uint Spare2;
        public uint Spare3;
        public uint Spare4;
        public IntPtr ReservedForOle;
        public uint WaitingOnLoaderLock;
        public IntPtr StackCommit;
        public IntPtr StackCommitMax;
        public IntPtr StackReserved;
    }
}
