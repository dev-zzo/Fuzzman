using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
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
        public uint DebuggeePid { get; private set; }

        public bool IsRunning { get { return this.continueDebugging; } }


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
            if (this.debuggerThread != null)
                return;

            this.debuggerThread = new Thread(this.DebuggerThreadCreateProcess);
            this.debuggerThread.Start(commandLine);
        }

        public void AttachToTarget(uint pid)
        {
            if (this.debuggerThread != null)
                return;

            this.debuggerThread = new Thread(this.DebuggerThreadAttach);
            this.debuggerThread.Start(pid);
        }

        public void TerminateTarget()
        {
            if (this.processInfo != null)
            {
                Kernel32.TerminateProcess(this.processInfo.Handle, 0xDEADDEAD);
            }
        }

        public void Stop()
        {
            this.continueDebugging = false;

            if (this.debuggerThread!= null && this.debuggerThread.IsAlive)
            {
                this.debuggerThread.Join();
            }
            this.debuggerThread = null;

            // Clean up, just in case.
            this.threadMap.Clear();
        }

        public void Dispose()
        {
            this.TerminateTarget();
            this.Stop();
        }

        #region Implementation details

        private ProcessInfo processInfo;
        private readonly IDictionary<uint, ThreadInfo> threadMap = new Dictionary<uint, ThreadInfo>();

        private Thread debuggerThread;
        private Exception debuggerException;
        private bool continueDebugging;
        private int ignoreBreakpointCounter;

        private void HandleDebuggerException(Exception ex)
        {
            this.debuggerException = ex;
            this.continueDebugging = false;
        }

        /// <summary>
        /// Debugger thread entry point, which creates the target process.
        /// </summary>
        private void DebuggerThreadCreateProcess(object p)
        {
            try
            {
                string commandLine = (string)p;

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

                this.DebuggeePid = (uint)procInfo.dwProcessId;
                this.DebuggerThreadLoop();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
            finally
            {
                this.debuggerThread = null;
            }
        }

        /// <summary>
        /// Debugger thread entry point, which attaches to an existing process.
        /// </summary>
        private void DebuggerThreadAttach(object p)
        {
            try
            {
                uint pid = (uint)p;

                if (!Kernel32.DebugActiveProcess(pid))
                {
                    throw new DebuggerException("Could not attach to process id " + pid + ".", Marshal.GetLastWin32Error());
                }

                Kernel32.DebugSetProcessKillOnExit(true);

                this.DebuggeePid = pid;
                this.DebuggerThreadLoop();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
            finally
            {
                this.debuggerThread = null;
            }
        }

        /// <summary>
        /// Main debugger loop. Waits for a debug event and dispatches it to corresponding handlers.
        /// </summary>
        private void DebuggerThreadLoop()
        {
            this.ignoreBreakpointCounter = 0;
            this.continueDebugging = true;

            int debugEventBufferLength = Marshal.SizeOf(typeof(DEBUG_EVENT));
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(debugEventBufferLength);
            try
            {
                while(this.continueDebugging)
                {
                    Kernel32.ZeroMemory(debugEventBuffer, (IntPtr)debugEventBufferLength);
                    bool ret = Kernel32.WaitForDebugEvent(debugEventBuffer, 0xFFFFFFFF);
                    if (!ret)
                        continue;

                    DEBUG_EVENT debugEvent = (DEBUG_EVENT)Marshal.PtrToStructure(debugEventBuffer, typeof(DEBUG_EVENT));
                    uint continueStatus = (uint)NTSTATUS.DBG_CONTINUE;
                    switch (debugEvent.dwDebugEventCode)
                    {
                        case DebugEventType.EXCEPTION_DEBUG_EVENT:
                            if (!this.OnExceptionDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, debugEvent.ExceptionInfo))
                            {
                                continueStatus = (uint)NTSTATUS.DBG_EXCEPTION_NOT_HANDLED;
                            }
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
                        throw new DebuggerException("Could not continue debugging.", Marshal.GetLastWin32Error());
                    }
                } // while
            }
            finally
            {
                Marshal.FreeHGlobal(debugEventBuffer);
            }
        }

        #region Debug event handlers

        /// <summary>
        /// Handler for an exception debugging event.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="tid"></param>
        /// <param name="info"></param>
        /// <returns>True to ignore the exception, false to pass the exception to the application.</returns>
        private bool OnExceptionDebugEvent(uint pid, uint tid, EXCEPTION_DEBUG_INFO info)
        {
            ExceptionDebugInfo convertedInfo = this.ConvertDebugInfo(info.ExceptionRecord);
            bool isFirstChance = info.dwFirstChance != 0;

            if (this.ignoreBreakpointCounter > 0 && convertedInfo.ExceptionCode == EXCEPTION_CODE.EXCEPTION_BREAKPOINT)
            {
                this.ignoreBreakpointCounter--;
                return true;
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

            return false;
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
            this.processInfo = new ProcessInfo();
            this.processInfo.Handle = info.hProcess;
            this.processInfo.PebLinearAddress = pbi.PebBaseAddress;

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

            this.processInfo = null;

            if (this.ProcessExitedEvent != null)
            {
                ProcessExitedEventParams e = new ProcessExitedEventParams();
                e.ProcessId = pid;
                e.ExitCode = info.dwExitCode;
                this.ProcessExitedEvent(this, e);
            }

            if (this.DebuggeePid == pid)
            {
                this.continueDebugging = false;
                this.threadMap.Clear();
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
