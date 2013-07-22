using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.OS.Windows.Interop;
using System.Text;

namespace Fuzzman.OS.Windows.Debuggers.Simple
{
    public sealed class SimpleDebugger : IDebugger
    {
        public uint DebuggeePid { get; private set; }

        public event DebuggerSharedLibraryLoadedEventHandler DebuggerSharedLibraryLoadedEvent;

        public event DebuggerSharedLibraryUnloadedEventHandler DebuggerSharedLibraryUnloadedEvent;

        public void CreateProcess(string commandLine)
        {
            this.debuggerThread = new Thread(this.DebuggerThreadCreateProcess);
            this.debuggerThread.Start(commandLine);
        }

        public void AttachToProcess(uint pid)
        {
            if (!Kernel32.DebugActiveProcess(pid))
            {
                throw new Exception("Could not attach to process id " + pid + ".");
            }

            this.DebuggeePid = pid;
            Kernel32.DebugSetProcessKillOnExit(true);
        }

        public void Stop()
        {
            this.isStopping = true;

            if (this.debuggerThread.IsAlive)
            {
                this.debuggerThread.Join();
            }
        }

        #region Implementation details

        private const uint DBG_CONTINUE = 0x00010002;
        private const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

        private readonly ILogger logger = LogManager.GetLogger("SimpleDebugger");

        private Thread debuggerThread = null;
        private bool isStopping = false;
        private bool debuggeeExited = false;

        private bool ShouldContinueDebugging()
        {
            if (this.isStopping)
                return false;
            if (this.debuggeeExited)
                return false;

            return true;
        }

        private void DebuggerThreadCreateProcess(object p)
        {
            string commandLine = (string)p;

            STARTUPINFO startupInfo = new STARTUPINFO();
            PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

            bool result = Kernel32.CreateProcess(
                null, // lpApplicationName 
                commandLine, // lpCommandLine 
                0, // lpProcessAttributes 
                0, // lpThreadAttributes 
                false, // bInheritHandles 
                1, // dwCreationFlags, DEBUG_PROCESS
                IntPtr.Zero, // lpEnvironment 
                null, // lpCurrentDirectory 
                ref startupInfo, // lpStartupInfo 
                out procInfo // lpProcessInformation 
                );
            if (!result)
            {
                Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                throw new Exception("Could not start process '" + commandLine + "'.", ex);
            }

            Kernel32.CloseHandle(procInfo.hProcess);
            Kernel32.CloseHandle(procInfo.hThread);
            Kernel32.DebugSetProcessKillOnExit(true);

            this.DebuggeePid = (uint)procInfo.dwProcessId;
            this.DebuggerThreadProcReal();
        }

        private void DebuggerThreadAttach(object p)
        {
            uint pid = (uint)p;

            if (!Kernel32.DebugActiveProcess(pid))
            {
                throw new Exception("Could not attach to process id " + pid + ".");
            }

            Kernel32.DebugSetProcessKillOnExit(true);

            this.DebuggeePid = pid;
            this.DebuggerThreadProcReal();
        }

        private void DebuggerThreadProcReal()
        {
            uint defaultTimeout = 100;

            for(;;)
            {
                this.PumpEvents(defaultTimeout);
            }
        }

        private void PumpEvents(uint timeoutMs)
        {
            int debugEventBufferLength = Marshal.SizeOf(typeof(DEBUG_EVENT)) + Marshal.SizeOf(typeof(EXCEPTION_DEBUG_INFO));
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(debugEventBufferLength);
            IntPtr debugEventUnion = debugEventBuffer + 12;
            Kernel32.ZeroMemory(debugEventBuffer, (IntPtr)debugEventBufferLength);
            try
            {
                bool ret = Kernel32.WaitForDebugEvent(debugEventBuffer, timeoutMs);
                if (ret)
                {
                    DebugEventType debugEventCode = (DebugEventType)Marshal.ReadInt32(debugEventBuffer, 0);
                    uint processId = (uint)Marshal.ReadInt32(debugEventBuffer, 4);
                    uint threadId = (uint)Marshal.ReadInt32(debugEventBuffer, 8);
                    bool continueExec = true;
                    switch (debugEventCode)
                    {
                        case DebugEventType.EXCEPTION_DEBUG_EVENT:
                            {
                                EXCEPTION_DEBUG_INFO info = (EXCEPTION_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXCEPTION_DEBUG_INFO));
                                continueExec = this.OnExceptionDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                            {
                                CREATE_THREAD_DEBUG_INFO info = (CREATE_THREAD_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(CREATE_THREAD_DEBUG_INFO));
                                continueExec = this.OnCreateThreadDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                            {
                                CREATE_PROCESS_DEBUG_INFO info = (CREATE_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(CREATE_PROCESS_DEBUG_INFO));
                                continueExec = this.OnCreateProcessDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
                            {
                                EXIT_THREAD_DEBUG_INFO info = (EXIT_THREAD_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXIT_THREAD_DEBUG_INFO));
                                continueExec = this.OnExitThreadDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
                            {
                                EXIT_PROCESS_DEBUG_INFO info = (EXIT_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXIT_PROCESS_DEBUG_INFO));
                                continueExec = this.OnExitProcessDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.LOAD_DLL_DEBUG_EVENT:
                            {
                                LOAD_DLL_DEBUG_INFO info = (LOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(LOAD_DLL_DEBUG_INFO));
                                continueExec = this.OnLoadDllDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
                            {
                                UNLOAD_DLL_DEBUG_INFO info = (UNLOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(UNLOAD_DLL_DEBUG_INFO));
                                continueExec = this.OnUnloadDllDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
                            {
                                OUTPUT_DEBUG_STRING_INFO info = (OUTPUT_DEBUG_STRING_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(OUTPUT_DEBUG_STRING_INFO));
                                continueExec = this.OnOutputDebugStringDebugEvent(processId, threadId, info);
                                break;
                            }
                        case DebugEventType.RIP_EVENT:
                            {
                                RIP_INFO info = (RIP_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(RIP_INFO));
                                continueExec = this.OnRipDebugEvent(processId, threadId, info);
                                break;
                            }
                        default:
                            throw new Exception("Unknown debugging event: 0x" + debugEventCode.ToString("X8"));
                    }

                    ret = Kernel32.ContinueDebugEvent(processId, threadId, continueExec ? DBG_CONTINUE : DBG_EXCEPTION_NOT_HANDLED);
                    if (!ret)
                    {
                        Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                        throw new Exception("Could not continue debugging.", ex);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(debugEventBuffer);
            }
        }

        private bool OnExceptionDebugEvent(uint pid, uint tid, EXCEPTION_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("Exception event: pid {0}, tid {1}", pid, tid));
            if (info.dwFirstChance == 0)
            {
                this.OnUnhandledExceptionEvent(pid, tid, info.ExceptionRecord);
                return false;
            }

            return true;
        }

        private bool OnCreateThreadDebugEvent(uint pid, uint tid, CREATE_THREAD_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("CreateThread event: pid {0}, tid {1}", pid, tid));

            return true;
        }

        private bool OnCreateProcessDebugEvent(uint pid, uint tid, CREATE_PROCESS_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("CreateProcess event: pid {0}, tid {1}", pid, tid));

            // Close the image handle.
            Kernel32.CloseHandle(info.hFile);
            // TODO: track the process/thread handles?
            return true;
        }

        private bool OnExitThreadDebugEvent(uint pid, uint tid, EXIT_THREAD_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("ExitThread event: pid {0}, tid {1}", pid, tid));
            return true;
        }

        private bool OnExitProcessDebugEvent(uint pid, uint tid, EXIT_PROCESS_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("ExitProcess event: pid {0}, tid {1}", pid, tid));

            if (this.DebuggeePid == pid)
            {
                this.debuggeeExited = true;
            }

            return true;
        }

        private bool OnLoadDllDebugEvent(uint pid, uint tid, LOAD_DLL_DEBUG_INFO info)
        {
            string imageName = "unknown"; // TODO
            this.logger.Debug(String.Format("LoadDll event: pid {0}, tid {1}: {2}", pid, tid, imageName));

            if (this.DebuggerSharedLibraryLoadedEvent != null)
            {
                DebuggerSharedLibraryLoadedEventParams e = new DebuggerSharedLibraryLoadedEventParams();
                e.ImageBase = info.lpBaseOfDll;
                e.ImageName = imageName;
                this.DebuggerSharedLibraryLoadedEvent(this, e);
            }

            Kernel32.CloseHandle(info.hFile);
            return true;
        }

        private bool OnUnloadDllDebugEvent(uint pid, uint tid, UNLOAD_DLL_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("UnloadDll event: pid {0}, tid {1}: {2}", pid, tid));

            if (this.DebuggerSharedLibraryUnloadedEvent != null)
            {
                DebuggerSharedLibraryUnloadedEventParams e = new DebuggerSharedLibraryUnloadedEventParams();
                e.ImageBase = info.lpBaseOfDll;
                this.DebuggerSharedLibraryUnloadedEvent(this, e);
            }

            return true;
        }

        private bool OnOutputDebugStringDebugEvent(uint pid, uint tid, OUTPUT_DEBUG_STRING_INFO info)
        {
            return true;
        }

        private bool OnRipDebugEvent(uint pid, uint tid, RIP_INFO info)
        {
            return true;
        }

        private void OnUnhandledExceptionEvent(uint pid, uint tid, EXCEPTION_RECORD info)
        {
        }

        #endregion
    }
}
