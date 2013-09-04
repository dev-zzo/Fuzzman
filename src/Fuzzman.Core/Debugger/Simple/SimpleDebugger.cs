using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Interop;

// Supplemental reading:
// http://www.alex-ionescu.com/dbgk-1.pdf
// http://www.alex-ionescu.com/dbgk-2.pdf
// http://www.alex-ionescu.com/dbgk-3.pdf

namespace Fuzzman.Core.Debugger.Simple
{
    public sealed class SimpleDebugger : IDebugger
    {
        public IDictionary<uint, ProcessInfo> Processes { get { return this.processMap; } }

        public IDictionary<uint, ThreadInfo> Threads { get { return this.threadMap; } }


        public event ExceptionEventHandler ExceptionEvent;

        public event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        public event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;

        public event ProcessCreatedEventHandler ProcessCreatedEvent;

        public event ThreadCreatedEventHandler ThreadCreatedEvent;

        public event ThreadExitedEventHandler ThreadExitedEvent;

        public event ProcessExitedEventHandler ProcessExitedEvent;


        public void CreateTarget(string commandLine)
        {
            STARTUPINFO startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

            bool result = Kernel32.CreateProcess(
                null, // lpApplicationName 
                commandLine, // lpCommandLine 
                IntPtr.Zero, // lpProcessAttributes 
                IntPtr.Zero, // lpThreadAttributes 
                false, // bInheritHandles 
                ProcessCreationFlags.DEBUG_PROCESS, // dwCreationFlags
                IntPtr.Zero, // lpEnvironment 
                null, // lpCurrentDirectory 
                ref startupInfo, // lpStartupInfo 
                out procInfo // lpProcessInformation 
                );
            if (!result)
            {
                throw new DebuggerException("Could not start process '" + commandLine + "'.", Marshal.GetLastWin32Error());
            }

            Kernel32.CloseHandle(procInfo.hThread);
            Kernel32.CloseHandle(procInfo.hProcess);
            Kernel32.DebugSetProcessKillOnExit(true);

            this.ignoreBreakpointCounter = 0;
        }

        public void AttachToTarget(uint pid)
        {
            if (!Kernel32.DebugActiveProcess(pid))
            {
                throw new DebuggerException("Could not attach to process id " + pid + ".", Marshal.GetLastWin32Error());
            }

            Kernel32.DebugSetProcessKillOnExit(true);

            this.ignoreBreakpointCounter = 0;
        }

        public void WaitAndDispatchEvent()
        {
            int debugEventBufferLength = Marshal.SizeOf(typeof(DEBUG_EVENT));
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(debugEventBufferLength);
            try
            {
                Kernel32.ZeroMemory(debugEventBuffer, (IntPtr)debugEventBufferLength);
                bool ret = Kernel32.WaitForDebugEvent(debugEventBuffer, 1000); // 1s waiting
                if (ret)
                {
                    DEBUG_EVENT debugEvent = (DEBUG_EVENT)Marshal.PtrToStructure(debugEventBuffer, typeof(DEBUG_EVENT));
                    NTSTATUS continueStatus = NTSTATUS.DBG_CONTINUE;
                    switch (debugEvent.dwDebugEventCode)
                    {
                        case DebugEventType.EXCEPTION_DEBUG_EVENT:
                            continueStatus = this.OnExceptionDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.ExceptionInfo);
                            break;
                        case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                            this.OnCreateThreadDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.CreateThreadInfo);
                            break;
                        case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                            this.OnCreateProcessDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.CreateProcessInfo);
                            break;
                        case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
                            this.OnExitThreadDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.ExitThreadInfo);
                            break;
                        case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
                            this.OnExitProcessDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.ExitProcessInfo);
                            break;
                        case DebugEventType.LOAD_DLL_DEBUG_EVENT:
                            this.OnLoadDllDebugEvent(debugEvent.dwProcessId, debugEvent.LoadDllInfo);
                            break;
                        case DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
                            this.OnUnloadDllDebugEvent(debugEvent.dwProcessId, debugEvent.UnloadDllInfo);
                            break;
                        case DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
                            this.OnOutputDebugStringDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.OutputDebugStringInfo);
                            break;
                        case DebugEventType.RIP_EVENT:
                            this.OnRipDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.RipInfo);
                            break;
                        default:
                            throw new DebuggerException("Unknown debugging event: 0x" + debugEvent.dwDebugEventCode.ToString("X8"));
                    }

                    ret = Kernel32.ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, continueStatus);
                    if (!ret)
                    {
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Debugger.Break();
                        }
                        throw new DebuggerException("Could not continue debugging.", Marshal.GetLastWin32Error());
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(debugEventBuffer);
            }
        }

        public void TerminateTarget()
        {
            // Have to make a copy -- the map changes when processes terminate.
            List<ProcessInfo> processes = new List<ProcessInfo>();
            foreach (ProcessInfo pi in this.Processes.Values)
            {
                processes.Add(pi);
            }
            foreach (ProcessInfo pi in processes)
            {
                //Kernel32.DebugActiveProcessStop(pi.Pid);
                bool res = Kernel32.TerminateProcess(pi.Handle, 0xDEADDEAD);
                if (!res)
                {
                }
            }
        }

        public void Dispose()
        {
            this.TerminateTarget();
        }

        #region Implementation details

        private readonly IDictionary<uint, ProcessInfo> processMap = new Dictionary<uint, ProcessInfo>();
        private readonly IDictionary<uint, ThreadInfo> threadMap = new Dictionary<uint, ThreadInfo>();

        private int ignoreBreakpointCounter;

        #region Debug event handlers

        private NTSTATUS OnExceptionDebugEvent(uint pid, uint tid, EXCEPTION_DEBUG_INFO info)
        {
            ExceptionDebugInfo convertedInfo = this.ConvertDebugInfo(info.ExceptionRecord);
            bool isFirstChance = info.dwFirstChance != 0;

            if (convertedInfo.ExceptionCode == EXCEPTION_CODE.EXCEPTION_BREAKPOINT && this.ignoreBreakpointCounter > 0)
            {
                this.ignoreBreakpointCounter--;
                return NTSTATUS.DBG_EXCEPTION_HANDLED;
            }

            if (convertedInfo.ExceptionCode == EXCEPTION_CODE.EXCEPTION_CPLUSPLUS && isFirstChance)
            {
                return NTSTATUS.DBG_EXCEPTION_NOT_HANDLED;
            }

            if (this.ExceptionEvent != null)
            {
                ExceptionEventParams e = new ExceptionEventParams();
                e.ProcessId = pid;
                e.ThreadId = tid;
                e.IsFirstChance = isFirstChance;
                e.Info = convertedInfo;
                this.ExceptionEvent(this, e);
            }

            return NTSTATUS.DBG_EXCEPTION_NOT_HANDLED;
        }

        private void OnCreateThreadDebugEvent(uint pid, uint tid, CREATE_THREAD_DEBUG_INFO info)
        {
            // Track the thread.
            // NOTE: Thread handle will be closed automatically by DbgSs.
            ThreadInfo threadInfo = new ThreadInfo();
            threadInfo.Handle = info.hThread;
            threadInfo.TebLinearAddress = info.lpThreadLocalBase;
            this.threadMap[tid] = threadInfo;

            if (this.ThreadCreatedEvent != null)
            {
                ThreadCreatedEventParams e = new ThreadCreatedEventParams();
                e.ProcessId = pid;
                e.ThreadId = tid;
                this.ThreadCreatedEvent(this, e);
            }
        }

        private void OnCreateProcessDebugEvent(uint pid, uint tid, CREATE_PROCESS_DEBUG_INFO info)
        {
            PROCESS_BASIC_INFORMATION pbi = NtdllHelpers.NtQueryProcessBasicInformation(info.hProcess);

            // Track the process.
            // NOTE: Process handle will be closed automatically by DbgSs.
            ProcessInfo processInfo = new ProcessInfo();
            processInfo.Pid = pid;
            processInfo.Handle = info.hProcess;
            processInfo.PebLinearAddress = pbi.PebBaseAddress;

            PEB peb = (PEB)DebuggerHelper.ReadTargetMemory(
                processInfo.Handle,
                processInfo.PebLinearAddress,
                typeof(PEB));
            RTL_USER_PROCESS_PARAMETERS rupp = (RTL_USER_PROCESS_PARAMETERS)DebuggerHelper.ReadTargetMemory(
                processInfo.Handle,
                peb.ProcessParameters,
                typeof(RTL_USER_PROCESS_PARAMETERS));
            IntPtr imagePathPtr = peb.ProcessParameters + (int)rupp.ImagePathName.Buffer;
            processInfo.ImagePath = DebuggerHelper.ReadNullTerminatedStringUnicode(processInfo.Handle, imagePathPtr);

            this.processMap[pid] = processInfo;

            if (this.ProcessCreatedEvent != null)
            {
                ProcessCreatedEventParams e = new ProcessCreatedEventParams();
                e.ProcessId = pid;
                this.ProcessCreatedEvent(this, e);
            }

            // Since the OS does not do this for us...
            CREATE_THREAD_DEBUG_INFO fakeInfo = new CREATE_THREAD_DEBUG_INFO();
            fakeInfo.hThread = info.hThread;
            fakeInfo.lpStartAddress = info.lpStartAddress;
            fakeInfo.lpThreadLocalBase = info.lpThreadLocalBase;
            this.OnCreateThreadDebugEvent(pid, tid, fakeInfo);

            // Close the image handle.
            Kernel32.CloseHandle(info.hFile);

            this.ignoreBreakpointCounter++;
        }

        private void OnExitThreadDebugEvent(uint pid, uint tid, EXIT_THREAD_DEBUG_INFO info)
        {
            this.threadMap.Remove(tid);

            if (this.ThreadExitedEvent != null)
            {
                ThreadExitedEventParams e = new ThreadExitedEventParams();
                e.ProcessId = pid;
                e.ThreadId = tid;
                e.ExitCode = info.dwExitCode;
                this.ThreadExitedEvent(this, e);
            }
        }

        private void OnExitProcessDebugEvent(uint pid, uint tid, EXIT_PROCESS_DEBUG_INFO info)
        {
            // Since the OS does not do this for us...
            EXIT_THREAD_DEBUG_INFO fakeInfo = new EXIT_THREAD_DEBUG_INFO();
            fakeInfo.dwExitCode = info.dwExitCode;
            this.OnExitThreadDebugEvent(pid, tid, fakeInfo);

            this.processMap.Remove(pid);

            if (this.ProcessExitedEvent != null)
            {
                ProcessExitedEventParams e = new ProcessExitedEventParams();
                e.ProcessId = pid;
                e.ExitCode = info.dwExitCode;
                this.ProcessExitedEvent(this, e);
            }
        }

        private void OnLoadDllDebugEvent(uint pid, LOAD_DLL_DEBUG_INFO info)
        {
            if (this.SharedLibraryLoadedEvent != null)
            {
                SharedLibraryLoadedEventParams e = new SharedLibraryLoadedEventParams();
                e.ProcessId = pid;
                e.ImageBase = info.lpBaseOfDll;
                this.SharedLibraryLoadedEvent(this, e);
            }

            Kernel32.CloseHandle(info.hFile);
        }

        private void OnUnloadDllDebugEvent(uint pid, UNLOAD_DLL_DEBUG_INFO info)
        {
            if (this.SharedLibraryUnloadedEvent != null)
            {
                SharedLibraryUnloadedEventParams e = new SharedLibraryUnloadedEventParams();
                e.ProcessId = pid;
                e.ImageBase = info.lpBaseOfDll;
                this.SharedLibraryUnloadedEvent(this, e);
            }
        }

        private void OnOutputDebugStringDebugEvent(uint pid, uint tid, OUTPUT_DEBUG_STRING_INFO info)
        {
            // TODO: Handle?
        }

        private void OnRipDebugEvent(uint pid, uint tid, RIP_INFO info)
        {
            // TODO: Handle?
        }

        #endregion

        #region Helpers

        private ExceptionDebugInfo ConvertDebugInfo(EXCEPTION_RECORD info)
        {
            ExceptionDebugInfo obj = null;
            switch (info.ExceptionCode)
            {
                case EXCEPTION_CODE.EXCEPTION_ACCESS_VIOLATION:
                    {
                        AccessViolationDebugInfo specific = new AccessViolationDebugInfo();
                        specific.Type = (AccessViolationDebugInfo.AccessType)info.ExceptionInformation0;
                        specific.TargetVA = info.ExceptionInformation1;
                        obj = specific;
                        break;
                    }
                default:
                    obj = new ExceptionDebugInfo();
                    break;
            }

            obj.ExceptionCode = info.ExceptionCode;
            obj.OffendingVA = info.ExceptionAddress;
            obj.IsContinuable = info.ExceptionFlags == 0;
            if (info.ExceptionRecord != IntPtr.Zero)
            {
                // TODO
            }

            return obj;
        }

        #endregion

        #endregion
    }
}
