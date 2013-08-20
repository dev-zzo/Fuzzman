using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Fuzzman.Core.Interop
{
    [Flags]
    public enum ProcessAccess : uint
    {
        /// <summary>Specifies all possible access flags for the process object.</summary>
        AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
        /// <summary>Enables usage of the process handle in the CreateRemoteThread function to create a thread in the process.</summary>
        CreateThread = 0x2,
        /// <summary>Enables usage of the process handle as either the source or target process in the DuplicateHandle function to duplicate a handle.</summary>
        DuplicateHandle = 0x40,
        /// <summary>Enables usage of the process handle in the GetExitCodeProcess and GetPriorityClass functions to read information from the process object.</summary>
        QueryInformation = 0x400,
        /// <summary>Enables usage of the process handle in the SetPriorityClass function to set the priority class of the process.</summary>
        SetInformation = 0x200,
        /// <summary>Enables usage of the process handle in the TerminateProcess function to terminate the process.</summary>
        Terminate = 0x1,
        /// <summary>Enables usage of the process handle in the VirtualProtectEx and WriteProcessMemory functions to modify the virtual memory of the process.</summary>
        VMOperation = 0x8,
        /// <summary>Enables usage of the process handle in the ReadProcessMemory function to' read from the virtual memory of the process.</summary>
        VMRead = 0x10,
        /// <summary>Enables usage of the process handle in the WriteProcessMemory function to write to the virtual memory of the process.</summary>
        VMWrite = 0x20,
        /// <summary>Enables usage of the process handle in any of the wait functions to wait for the process to terminate.</summary>
        Synchronize = 0x100000
    }

    [Flags]
    public enum ProcessCreationFlags : uint
    {
        NONE = 0,
        DEBUG_PROCESS = 0x00000001,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        CREATE_SUSPENDED = 0x00000004,
        DETACHED_PROCESS = 0x00000008,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        CREATE_SEPARATE_WOW_VDM = 0x00000800,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        INHERIT_PARENT_AFFINITY = 0x00010000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NO_WINDOW = 0x08000000,
    }

    [Flags]
    public enum FileMapProtection : uint
    {
        PageReadOnly = 0x02,
        PageReadWrite = 0x04,
        PageWriteCopy = 0x08,
        PageExecuteRead = 0x20,
        PageExecuteReadWrite = 0x40,
        SectionCommit = 0x8000000,
        SectionImage = 0x1000000,
        SectionNoCache = 0x10000000,
        SectionReserve = 0x4000000,
    }

    [Flags]
    public enum FileMapAccess : uint
    {
        Copy = 0x0001,
        Write = 0x0002,
        Read = 0x0004,
        AllAccess = 0x001f,
        Execute = 0x0020,
    }

    public static class Kernel32
    {
        #region Processes and threads

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            ProcessAccess dwDesiredAccess,
            bool bInheritHandle,
            uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            UInt32 nSize,
            out uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern bool GetThreadContext(
            IntPtr hThread,
            ref CONTEXT lpContext);

        [DllImport("kernel32.dll")]
        public static extern bool GetThreadSelectorEntry(
            IntPtr hThread,
            uint dwSelector,
            ref LDT_ENTRY lpSelectorEntry);

        [DllImport("kernel32.dll")]
        public static extern bool SetThreadContext(
            IntPtr hThread,
            [In] ref CONTEXT lpContext);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(
            IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(
            IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern bool TerminateProcess(
            IntPtr hProcess,
            uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushInstructionCache(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            uint dwSize);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(
            string moduleName);

        [DllImport("kernel32.dll")]
        public static extern bool GetProcessTimes(
            IntPtr hProcess,
            out UInt64 lpCreationTime,
            out UInt64 lpExitTime,
            out UInt64 lpKernelTime,
            out UInt64 lpUserTime);

        #endregion

        #region File mapping

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            IntPtr lpAttributes,
            FileMapProtection flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            IntPtr lpName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr MapViewOfFileEx(
            IntPtr hFileMappingObject,
            FileMapAccess dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            IntPtr dwNumberOfBytesToMap,
            IntPtr lpBaseAddress);

        [DllImport("kernel32.dll")]
        public static extern bool UnmapViewOfFile(
            IntPtr lpBaseAddress);

        [DllImport("kernel32.dll")]
        public static extern bool FlushViewOfFile(
            IntPtr lpBaseAddress,
            IntPtr dwNumberOfBytesToFlush);

        [DllImport("kernel32.dll")]
        public static extern bool GetFileSizeEx(
            IntPtr hFile,
            out UInt64 lpFileSize);

        #endregion

        #region Debugging

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent", SetLastError = true)]
        public static extern bool WaitForDebugEvent(
            IntPtr lpDebugEvent,
            uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern bool DebugActiveProcess(
            uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool DebugActiveProcessStop(
            uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ContinueDebugEvent(
            uint dwProcessId,
            uint dwThreadId,
            NTSTATUS dwContinueStatus);

        [DllImport("kernel32.dll")]
        public static extern bool DebugSetProcessKillOnExit(
            bool KillOnExit);

        #endregion

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(
            IntPtr dest,
            IntPtr size);

        [DllImport("kernel32.dll")]
        public static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr Arguments);
    }

    public static class Kernel32Helpers
    {
        public static string GetSystemMessage(int errorCode)
        {
            int capacity = 512;
            int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            StringBuilder sb = new StringBuilder(capacity);
            Kernel32.FormatMessage(
                FORMAT_MESSAGE_FROM_SYSTEM,
                IntPtr.Zero,
                errorCode,
                0,
                sb,
                sb.Capacity,
                IntPtr.Zero);
            int i = sb.Length;
            while (i > 0 && (sb[i - 1] == '\r' || sb[i - 1] == '\n'))
                i--;
            sb.Length = i;
            return sb.ToString();
        }
    }
}
