using System;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop
{
    public enum DBGKM_APINUMBER : uint
    {
        DbgKmExceptionApi = 0,
        DbgKmCreateThreadApi = 1,
        DbgKmCreateProcessApi = 2,
        DbgKmExitThreadApi = 3,
        DbgKmExitProcessApi = 4,
        DbgKmLoadDllApi = 5,
        DbgKmUnloadDllApi = 6,
        DbgKmErrorReportApi = 7,
        DbgKmMaxApiNumber = 8,
    }

    public enum DEBUGOBJECTINFOCLASS
    {
        DebugObjectUnusedInformation,
        DebugObjectKillProcessOnExitInformation
    }

    public enum DBG_STATE : uint
    {
        DbgIdle,
        DbgReplyPending,
        DbgCreateThreadStateChange,
        DbgCreateProcessStateChange,
        DbgExitThreadStateChange,
        DbgExitProcessStateChange,
        DbgExceptionStateChange,
        DbgBreakpointStateChange,
        DbgSingleStepStateChange,
        DbgLoadDllStateChange,
        DbgUnloadDllStateChange
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_EXCEPTION
    {
        public EXCEPTION_RECORD ExceptionRecord;
        public uint FirstChance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_CREATE_THREAD
    {
        public uint SubSystemKey;
        public IntPtr StartAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_CREATE_PROCESS
    {
        public uint SubSystemKey;
        public IntPtr FileHandle;
        public IntPtr BaseOfImage;
        public uint DebugInfoFileOffset;
        public uint DebugInfoSize;
        public DBGKM_CREATE_THREAD InitialThread;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_EXIT_THREAD
    {
        public NTSTATUS ExitStatus;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_EXIT_PROCESS
    {
        public NTSTATUS ExitStatus;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_LOAD_DLL
    {
        public IntPtr FileHandle;
        public IntPtr BaseOfDll;
        public uint DebugInfoFileOffset;
        public uint DebugInfoSize;
        public IntPtr NamePointer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGKM_UNLOAD_DLL
    {
        public IntPtr BaseAddress;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct DBGUI_CREATE_THREAD
    {
        public IntPtr ThreadHandle;
        public DBGKM_CREATE_THREAD NewThread;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DBGUI_CREATE_PROCESS
    {
        public IntPtr ProcessHandle;
        public IntPtr ThreadHandle;
        public DBGKM_CREATE_PROCESS NewProcess;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DBGUI_WAIT_STATE_CHANGE
    {
        [FieldOffset(0x00)]
        DBG_STATE NewState;

        [FieldOffset(0x04)]
        CLIENT_ID AppClientId;

        [FieldOffset(0x0C)]
        DBGUI_CREATE_THREAD CreateThread;

        [FieldOffset(0x0C)]
        DBGUI_CREATE_PROCESS CreateProcess;

        [FieldOffset(0x0C)]
        DBGKM_EXIT_THREAD ExitThread;

        [FieldOffset(0x0C)]
        DBGKM_EXIT_PROCESS ExitProcess;

        [FieldOffset(0x0C)]
        DBGKM_EXCEPTION Exception;

        [FieldOffset(0x0C)]
        DBGKM_LOAD_DLL LoadDll;

        [FieldOffset(0x0C)]
        DBGKM_UNLOAD_DLL UnloadDll;
    }
}
