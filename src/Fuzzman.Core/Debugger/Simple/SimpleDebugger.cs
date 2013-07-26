using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Debugger.Simple
{
    public sealed class SimpleDebugger : IDebugger
    {
        public uint DebuggeePid { get; private set; }

        public bool IsRunning { get { return this.continueDebugging; } }


        public IDictionary<uint, ThreadInfo> Threads { get { return this.threadMap; } }

        public IDictionary<IntPtr, ModuleInfo> Modules { get { return this.moduleMap; } }


        public event ExceptionEventHandler ExceptionEvent;

        public event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        public event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;

        public event ProcessCreatedEventHandler ProcessCreatedEvent;

        public event ThreadCreatedEventHandler ThreadCreatedEvent;

        public event ThreadExitedEventHandler ThreadExitedEvent;

        public event ProcessExitedEventHandler ProcessExitedEvent;


        public void StartTarget(string commandLine)
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

        public CONTEXT GetThreadContext(uint tid)
        {
            return GetThreadContext(this.threadMap[tid].Handle);
        }

        public CONTEXT GetThreadContext(IntPtr threadHandle)
        {
            CONTEXT context = new CONTEXT();
            context.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
            if (!Kernel32.GetThreadContext(threadHandle, ref context))
            {
                throw new Exception("Failed to retrieve the thread context.");
            }
            return context;
        }

        public LDT_ENTRY GetThreadLdtEntry(uint tid, uint selector)
        {
            return GetThreadLdtEntry(this.threadMap[tid].Handle, selector);
        }

        public LDT_ENTRY GetThreadLdtEntry(IntPtr threadHandle, uint selector)
        {
            LDT_ENTRY entry = new LDT_ENTRY();
            if (!Kernel32.GetThreadSelectorEntry(threadHandle, selector, ref entry))
            {
                throw new Exception("Failed to retrieve the thread selector entry.");
            }
            return entry;
        }

        public void TerminateTarget()
        {
            if (this.targetProcessHandle != IntPtr.Zero)
            {
                Kernel32.TerminateProcess(this.targetProcessHandle, 0xDEADDEAD);
                Kernel32.CloseHandle(this.targetProcessHandle);
                this.targetProcessHandle = IntPtr.Zero;
            }
        }

        public void Stop()
        {
            this.continueDebugging = false;

            if (this.debuggerThread.IsAlive)
            {
                this.debuggerThread.Join();
            }
            this.debuggerThread = null;
        }

        public void Dispose()
        {
            this.TerminateTarget();
            this.Stop();
        }

        #region Implementation details

        private const uint DBG_CONTINUE = 0x00010002;
        private const uint DBG_EXCEPTION_NOT_HANDLED = 0x80010001;

        private readonly ILogger logger = LogManager.GetLogger("SimpleDebugger");

        /// <summary>
        /// Maps threadId -> ThreadInfo.
        /// </summary>
        private readonly IDictionary<uint, ThreadInfo> threadMap = new Dictionary<uint, ThreadInfo>();

        /// <summary>
        /// Maps module base address -> ModuleInfo.
        /// </summary>
        private readonly IDictionary<IntPtr, ModuleInfo> moduleMap = new Dictionary<IntPtr, ModuleInfo>();

        private Thread debuggerThread = null;
        private Exception debuggerException = null;
        private bool continueDebugging = true;
        private bool ignoreBreakpoint = false;

        private IntPtr targetProcessHandle;
        private IntPtr targetProcessPebAddress;

        private void HandleDebuggerException(Exception ex)
        {
            this.logger.Fatal(String.Format("Debugger thread crashed with exception:\r\n{0}", ex.ToString()));
            this.debuggerException = ex;
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
                PROCESS_INFORMATION procInfo = new PROCESS_INFORMATION();

                bool result = Kernel32.CreateProcess(
                    null, // lpApplicationName 
                    commandLine, // lpCommandLine 
                    IntPtr.Zero, // lpProcessAttributes 
                    IntPtr.Zero, // lpThreadAttributes 
                    false, // bInheritHandles 
                    ProcessCreationFlags.DEBUG_PROCESS | ProcessCreationFlags.DEBUG_ONLY_THIS_PROCESS, // dwCreationFlags
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

                this.targetProcessHandle = procInfo.hProcess;
                Kernel32.CloseHandle(procInfo.hThread);
                Kernel32.DebugSetProcessKillOnExit(true);

                this.ignoreBreakpoint = true;
                this.continueDebugging = true;

                this.DebuggeePid = (uint)procInfo.dwProcessId;
                this.DebuggerThreadLoop();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
        }

        /// <summary>
        /// Debugger thread entry point which attaches to an existing process.
        /// </summary>
        private void DebuggerThreadAttach(object p)
        {
            try
            {
                uint pid = (uint)p;

                if (!Kernel32.DebugActiveProcess(pid))
                {
                    throw new Exception("Could not attach to process id " + pid + ".");
                }

                Kernel32.DebugSetProcessKillOnExit(true);

                this.ignoreBreakpoint = false;
                this.continueDebugging = true;

                this.DebuggeePid = pid;
                this.DebuggerThreadLoop();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
        }

        private void DebuggerThreadLoop()
        {
            this.logger.Info("Debugger thread enters main loop.");

            int debugEventBufferLength = Marshal.SizeOf(typeof(DEBUG_EVENT)) + Marshal.SizeOf(typeof(EXCEPTION_DEBUG_INFO));
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(debugEventBufferLength);
            IntPtr debugEventUnion = debugEventBuffer + 12;
            try
            {
                for (; ; )
                {
                    if (!this.continueDebugging)
                        break;

                    Kernel32.ZeroMemory(debugEventBuffer, (IntPtr)debugEventBufferLength);
                    bool ret = Kernel32.WaitForDebugEvent(debugEventBuffer, 0xFFFFFFFF);
                    if (ret)
                    {
                        DebugEventType debugEventCode = (DebugEventType)Marshal.ReadInt32(debugEventBuffer, 0);
                        uint processId = (uint)Marshal.ReadInt32(debugEventBuffer, 4);
                        uint threadId = (uint)Marshal.ReadInt32(debugEventBuffer, 8);
                        uint continueStatus = DBG_CONTINUE;
                        switch (debugEventCode)
                        {
                            case DebugEventType.EXCEPTION_DEBUG_EVENT:
                                {
                                    EXCEPTION_DEBUG_INFO info = (EXCEPTION_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXCEPTION_DEBUG_INFO));
                                    bool continueExec = this.OnExceptionDebugEvent(processId, threadId, info);
                                    if (!continueExec)
                                    {
                                        continueStatus = DBG_EXCEPTION_NOT_HANDLED;
                                    }
                                    break;
                                }
                            case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                                {
                                    CREATE_THREAD_DEBUG_INFO info = (CREATE_THREAD_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(CREATE_THREAD_DEBUG_INFO));
                                    this.OnCreateThreadDebugEvent(processId, threadId, info);
                                    break;
                                }
                            case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                                {
                                    CREATE_PROCESS_DEBUG_INFO info = (CREATE_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(CREATE_PROCESS_DEBUG_INFO));
                                    this.OnCreateProcessDebugEvent(processId, threadId, info);
                                    break;
                                }
                            case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
                                {
                                    EXIT_THREAD_DEBUG_INFO info = (EXIT_THREAD_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXIT_THREAD_DEBUG_INFO));
                                    this.OnExitThreadDebugEvent(processId, threadId, info);
                                    break;
                                }
                            case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
                                {
                                    EXIT_PROCESS_DEBUG_INFO info = (EXIT_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(EXIT_PROCESS_DEBUG_INFO));
                                    this.OnExitProcessDebugEvent(processId, threadId, info);
                                    break;
                                }
                            case DebugEventType.LOAD_DLL_DEBUG_EVENT:
                                {
                                    LOAD_DLL_DEBUG_INFO info = (LOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(LOAD_DLL_DEBUG_INFO));
                                    this.OnLoadDllDebugEvent(processId, info);
                                    break;
                                }
                            case DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
                                {
                                    UNLOAD_DLL_DEBUG_INFO info = (UNLOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(UNLOAD_DLL_DEBUG_INFO));
                                    this.OnUnloadDllDebugEvent(processId, info);
                                    break;
                                }
                            case DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
                                {
                                    OUTPUT_DEBUG_STRING_INFO info = (OUTPUT_DEBUG_STRING_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(OUTPUT_DEBUG_STRING_INFO));
                                    this.OnOutputDebugStringDebugEvent(processId, threadId, info);
                                    break;
                                }
                            case DebugEventType.RIP_EVENT:
                                {
                                    RIP_INFO info = (RIP_INFO)Marshal.PtrToStructure(debugEventUnion, typeof(RIP_INFO));
                                    this.OnRipDebugEvent(processId, threadId, info);
                                    break;
                                }
                            default:
                                throw new Exception("Unknown debugging event: 0x" + debugEventCode.ToString("X8"));
                        }

                        ret = Kernel32.ContinueDebugEvent(processId, threadId, continueStatus);
                        if (!ret)
                        {
                            Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());
                            throw new Exception("Could not continue debugging.", ex);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(debugEventBuffer);
                if (this.targetProcessHandle != IntPtr.Zero)
                {
                    Kernel32.CloseHandle(this.targetProcessHandle);
                    this.targetProcessHandle = IntPtr.Zero;
                }
            }
            this.logger.Info("Debugger thread exited.");
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
            ExceptionDebugInfo marshaledInfo = this.MarshalDebugInfo(info.ExceptionRecord);
            bool isFirstChance = info.dwFirstChance == 1;

            //this.logger.Info(String.Format("Exception in thread {0} ({1}-chance):\r\n{2}",
            //    tid,
            //    isFirstChance ? "first" : "second",
            //    marshaledInfo.ToString()));

            if (this.ignoreBreakpoint && marshaledInfo.ExceptionCode == EXCEPTION_CODE.EXCEPTION_BREAKPOINT)
            {
                this.ignoreBreakpoint = false;
                return true;
            }

            if (this.ExceptionEvent != null)
            {
                ExceptionEventParams e = new ExceptionEventParams();
                e.ProcessId = pid;
                e.ThreadId = tid;
                e.IsFirstChance = isFirstChance;
                e.Info = marshaledInfo;
                this.ExceptionEvent(this, e);
            }

            return false;
        }

        private void OnCreateThreadDebugEvent(uint pid, uint tid, CREATE_THREAD_DEBUG_INFO info)
        {
            CONTEXT threadContext = this.GetThreadContext(info.hThread);
            LDT_ENTRY tebLdtEntry = this.GetThreadLdtEntry(info.hThread, threadContext.SegFs);

            // Track the thread.
            ThreadInfo threadInfo = new ThreadInfo();
            threadInfo.Handle = info.hThread;
            threadInfo.TebLinearAddress = (IntPtr)tebLdtEntry.Base;
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

            // Locate the process PEB. Very hacky.
            ThreadInfo threadInfo = this.threadMap[tid];
            this.targetProcessPebAddress = (IntPtr)DebuggerHelper.ReadTargetMemory(
                this.targetProcessHandle,
                threadInfo.TebLinearAddress + 0x30,
                typeof(IntPtr));

            // Close the image handle.
            Kernel32.CloseHandle(info.hFile);
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
            }
        }

        private void OnLoadDllDebugEvent(uint pid, LOAD_DLL_DEBUG_INFO info)
        {
            this.UpdateModuleList();

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

            this.moduleMap.Remove(info.lpBaseOfDll);
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

        private void UpdateModuleList()
        {
            PEB peb = (PEB)DebuggerHelper.ReadTargetMemory(
                this.targetProcessHandle,
                this.targetProcessPebAddress,
                typeof(PEB));
            if (peb.LoaderData == IntPtr.Zero)
            {
                return;
            }

            PEB_LDR_DATA pebLoaderData = (PEB_LDR_DATA)DebuggerHelper.ReadTargetMemory(
                this.targetProcessHandle,
                peb.LoaderData,
                typeof(PEB_LDR_DATA));
            IntPtr headAnchorVA = peb.LoaderData + Marshal.OffsetOf(typeof(PEB_LDR_DATA), "InLoadOrderModuleList").ToInt32();
            IntPtr currentAnchorVA = pebLoaderData.InLoadOrderModuleList.Flink;
            while (currentAnchorVA != headAnchorVA)
            {
                LDR_MODULE module = (LDR_MODULE)DebuggerHelper.ReadTargetMemory(
                    this.targetProcessHandle,
                    currentAnchorVA - Marshal.OffsetOf(typeof(LDR_MODULE), "InLoadOrderModuleList").ToInt32(),
                    typeof(LDR_MODULE));

                if (!this.moduleMap.ContainsKey(module.BaseAddress))
                {
                    ModuleInfo info = new ModuleInfo();
                    info.BaseAddress = module.BaseAddress;
                    info.MappedSize = module.SizeOfImage;
                    info.Name = DebuggerHelper.ReadUnicodeString(
                        this.targetProcessHandle,
                        module.BaseDllName);
                    info.FullPath = DebuggerHelper.ReadUnicodeString(
                        this.targetProcessHandle,
                        module.FullDllName);

                    this.moduleMap[info.BaseAddress] = info;
                }

                currentAnchorVA = module.InLoadOrderModuleList.Flink;
            }
        }

        #region Marshaling helpers

        private ExceptionDebugInfo MarshalDebugInfo(EXCEPTION_RECORD info)
        {
            ExceptionDebugInfo obj = null;
            switch (info.ExceptionCode)
            {
                case EXCEPTION_CODE.EXCEPTION_ACCESS_VIOLATION:
                    {
                        AccessViolationDebugInfo specific = new AccessViolationDebugInfo();
                        specific.Type = (AccessViolationDebugInfo.AccessType)info.ExceptionInformation[0];
                        specific.TargetVA = info.ExceptionInformation[1];
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
                this.logger.Warning("The exception has a nested one, but I can't yet handle that.");
            }

            return obj;
        }

        #endregion

        #endregion
    }
}
