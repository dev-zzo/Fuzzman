using System;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop
{
    public enum DebugEventType : uint
    {
        EXCEPTION_DEBUG_EVENT = 1,
        CREATE_THREAD_DEBUG_EVENT = 2,
        CREATE_PROCESS_DEBUG_EVENT = 3,
        EXIT_THREAD_DEBUG_EVENT = 4,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        LOAD_DLL_DEBUG_EVENT = 6,
        UNLOAD_DLL_DEBUG_EVENT = 7,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        RIP_EVENT = 9,
    };

    public struct DEBUG_EVENT
    {
        public DebugEventType dwDebugEventCode;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_DEBUG_INFO
    {
        public EXCEPTION_RECORD ExceptionRecord;
        public uint dwFirstChance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_THREAD_DEBUG_INFO
    {
        public IntPtr hThread;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_PROCESS_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr hProcess;
        public IntPtr hThread;
        public IntPtr lpBaseOfImage;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpThreadLocalBase;
        public IntPtr lpStartAddress;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_THREAD_DEBUG_INFO
    {
        public uint dwExitCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXIT_PROCESS_DEBUG_INFO
    {
        public uint dwExitCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LOAD_DLL_DEBUG_INFO
    {
        public IntPtr hFile;
        public IntPtr lpBaseOfDll;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNLOAD_DLL_DEBUG_INFO
    {
        public IntPtr lpBaseOfDll;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OUTPUT_DEBUG_STRING_INFO
    {
        public IntPtr lpDebugStringData;
        public ushort fUnicode;
        public ushort nDebugStringLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RIP_INFO
    {
        public uint dwError;
        public uint dwType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LDT_ENTRY
    {
        public ushort LimitLow;
        public ushort BaseLow;
        public byte BaseMid;
        public ushort Flags;
        public byte BaseHi;

        public uint Base
        {
            get
            {
                return (uint)(this.BaseLow + (this.BaseMid << 16) + (this.BaseHi << 24));
            }
        }

        public uint Limit
        {
            get
            {
                return (uint)(this.LimitLow + ((this.Flags >> 16) & 0xF));
            }
        }
    }
}
