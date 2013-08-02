using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Fuzzman.Core.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_ATTRIBUTES
    {
        public void Initialize()
        {
            this.Length = (uint)Marshal.SizeOf(this);
        }

        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    public enum KWAIT_REASON : uint
    {
        Executive = 0,
        FreePage = 1,
        PageIn = 2,
        PoolAllocation = 3,
        DelayExecution = 4,
        Suspended = 5,
        UserRequest = 6,
        WrExecutive = 7,
        WrFreePage = 8,
        WrPageIn = 9,
        WrPoolAllocation = 10,
        WrDelayExecution = 11,
        WrSuspended = 12,
        WrUserRequest = 13,
        WrEventPair = 14,
        WrQueue = 15,
        WrLpcReceive = 16,
        WrLpcReply = 17,
        WrVirtualMemory = 18,
        WrPageOut = 19,
        WrRendezvous = 20,
        Spare2 = 21,
        Spare3 = 22,
        Spare4 = 23,
        Spare5 = 24,
        WrCalloutStack = 25,
        WrKernel = 26,
        WrResource = 27,
        WrPushLock = 28,
        WrMutex = 29,
        WrQuantumEnd = 30,
        WrDispatchInt = 31,
        WrPreempted = 32,
        WrYieldExecution = 33,
        WrFastMutex = 34,
        WrGuardedMutex = 35,
        WrRundown = 36,
        MaximumWaitReason = 37
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_BASIC_INFORMATION {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public UIntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    public enum ProcessInformationClass : uint
    {
        ProcessBasicInformation = 0,
        ProcessDebugPort = 7,
        ProcessWow64Information = 26,
        ProcessImageFileName = 27,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        public UInt64 ReadOperationCount;
        public UInt64 WriteOperationCount;
        public UInt64 OtherOperationCount;
        public UInt64 ReadTransferCount;
        public UInt64 WriteTransferCount;
        public UInt64 OtherTransferCount;
    }

    // hmm... intptr?
    [StructLayout(LayoutKind.Sequential)]
    public struct VM_COUNTERS
    {
        public uint PeakVirtualSize;
        public uint VirtualSize;
        public uint PageFaultCount;
        public uint PeakWorkingSetSize;
        public uint WorkingSetSize;
        public uint QuotaPeakPagedPoolUsage;
        public uint QuotaPagedPoolUsage;
        public uint QuotaPeakNonPagedPoolUsage;
        public uint QuotaNonPagedPoolUsage;
        public uint PagefileUsage;
        public uint PeakPagefileUsage;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_THREAD
    {
        public UInt64 KernelTime;
        public UInt64 UserTime;
        public UInt64 CreateTime;
        public uint WaitTime;
        public IntPtr StartAddress;
        public CLIENT_ID ClientId;
        public int Priority;
        public int BasePriority;
        public uint ContextSwitchCount;
        public uint State;
        public KWAIT_REASON WaitReason;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_PROCESS_INFORMATION
    {
        public uint NextEntryOffset;
        public uint NumberOfThreads;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public UInt64 Reserved1a;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public UInt64 Reserved1b;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public UInt64 Reserved1c;
        public UInt64 CreateTime;
        public UInt64 UserTime;
        public UInt64 KernelTime;
        public UNICODE_STRING ImageName;
        public int BasePriority;
        public uint ProcessId;
        public uint InheritedFromProcessId;
        public uint HandleCount;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public uint Reserved2a;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public uint Reserved2b;
        public uint PrivatePageCount;
        public VM_COUNTERS VirtualMemoryCounters;
        public IO_COUNTERS IoCounters;
        // Array of SYSTEM_THREAD follows.
    }

    public enum SystemInformationClass : uint
    {
        SystemBasicInformation,
        SystemProcessorInformation,
        SystemPerformanceInformation,
        SystemTimeOfDayInformation,
        SystemPathInformation,
        SystemProcessInformation,
        SystemCallCountInformation,
        SystemDeviceInformation,
        SystemProcessorPerformanceInformation,
        SystemFlagsInformation,
        SystemCallTimeInformation,
        SystemModuleInformation,
        SystemLocksInformation,
        SystemStackTraceInformation,
        SystemPagedPoolInformation,
        SystemNonPagedPoolInformation,
        SystemHandleInformation,
        SystemObjectInformation,
        SystemPageFileInformation,
        SystemVdmInstemulInformation,
        SystemVdmBopInformation,
        SystemFileCacheInformation,
        SystemPoolTagInformation,
        SystemInterruptInformation,
        SystemDpcBehaviorInformation,
        SystemFullMemoryInformation,
        SystemLoadGdiDriverInformation,
        SystemUnloadGdiDriverInformation,
        SystemTimeAdjustmentInformation,
        SystemSummaryMemoryInformation,
        SystemNextEventIdInformation,
        SystemEventIdsInformation,
        SystemCrashDumpInformation,
        SystemExceptionInformation,
        SystemCrashDumpStateInformation,
        SystemKernelDebuggerInformation,
        SystemContextSwitchInformation,
        SystemRegistryQuotaInformation,
        SystemExtendServiceTableInformation,
        SystemPrioritySeperation,
        SystemPlugPlayBusInformation,
        SystemDockInformation,
        SystemPowerInformation,
        SystemProcessorSpeedInformation,
        SystemCurrentTimeZoneInformation,
        SystemLookasideInformation,
    }

    public static class Ntdll
    {
        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtQueryInformationProcess(
            IntPtr ProcessHandle,
            ProcessInformationClass ProcessInformationClass,
            IntPtr ProcessInformation,
            IntPtr ProcessInformationLength,
            out IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtQuerySystemInformation(
            SystemInformationClass SystemInformationClass,
            IntPtr SystemInformation,
            IntPtr SystemInformationLength,
            out IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS ZwCreateDebugObject(
            ref IntPtr DebugHandle,
            uint DesiredAccess,
            ref OBJECT_ATTRIBUTES ObjectAttributes,
            uint Flags);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtDebugActiveProcess(
            IntPtr ProcessHandle,
            IntPtr DebugHandle);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtRemoveProcessDebug(
            IntPtr ProcessHandle,
            IntPtr DebugHandle);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS ZwDebugContinue(
            IntPtr DebugHandle,
            ref CLIENT_ID ClientId,
            NTSTATUS ContinueStatus);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtWaitForDebugEvent(
            IntPtr DebugHandle,
            bool Alertable,
            ref UInt64 Timeout,
            out DBGUI_WAIT_STATE_CHANGE StateChange);
    }

    public static class NtdllHelpers
    {
        public static PROCESS_BASIC_INFORMATION NtQueryProcessBasicInformation(
            IntPtr ProcessHandle)
        {
            IntPtr pbi = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)));
            IntPtr returnLength;

            NTSTATUS status = Ntdll.NtQueryInformationProcess(
                ProcessHandle,
                ProcessInformationClass.ProcessBasicInformation,
                pbi,
                (IntPtr)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)),
                out returnLength);

            PROCESS_BASIC_INFORMATION result = (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pbi, typeof(PROCESS_BASIC_INFORMATION));

            Marshal.FreeHGlobal(pbi);

            return result;
        }
    }
}
