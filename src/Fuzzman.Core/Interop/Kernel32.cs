using System;
using System.Runtime.InteropServices;

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

    public static class Kernel32
    {
        #region Processes and threads

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            int lpProcessAttributes,
            int lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(
            ProcessAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] buffer,
            UInt32 size,
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

        #endregion

        #region Debugging

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitForDebugEvent(IntPtr lpDebugEvent, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern bool DebugActiveProcess(uint dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool DebugActiveProcessStop(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ContinueDebugEvent(
            uint dwProcessId,
            uint dwThreadId,
            uint dwContinueStatus);

        [DllImport("kernel32.dll")]
        public static extern bool DebugSetProcessKillOnExit(
            bool KillOnExit);

        #endregion

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, IntPtr size);
    }
}
