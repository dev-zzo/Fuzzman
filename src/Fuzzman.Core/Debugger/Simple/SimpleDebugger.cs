﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Interop;
using System.Collections;
using System.Collections.Generic;

namespace Fuzzman.Core.Debugger.Simple
{
    public sealed class SimpleDebugger : IDebugger
    {
        public uint DebuggeePid { get; private set; }

        public event ExceptionEventHandler ExceptionEvent;

        public event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        public event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;

        public void CreateTarget(string commandLine)
        {
            this.debuggerThread = new Thread(this.DebuggerThreadCreateProcess);
            this.debuggerThread.Start(commandLine);
        }

        public void AttachToTarget(uint pid)
        {
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
                this.debuggeeExited = true;
                Kernel32.CloseHandle(this.targetProcessHandle);
                this.targetProcessHandle = IntPtr.Zero;
            }
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

        private readonly IDictionary<uint, ThreadInfo> threadMap = new Dictionary<uint, ThreadInfo>();
        private readonly IDictionary<IntPtr, ModuleInfo> moduleMap = new Dictionary<IntPtr, ModuleInfo>();

        private Thread debuggerThread = null;
        private bool isStopping = false;
        private bool debuggeeExited = false;
        private bool ignoreBreakpoint = false;
        private Exception debuggerException = null;

        private IntPtr targetProcessHandle;
        private IntPtr targetProcessPebAddress;

        private void HandleDebuggerException(Exception ex)
        {
            this.debuggerException = ex;
        }

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

                this.targetProcessHandle = procInfo.hProcess;
                Kernel32.CloseHandle(procInfo.hThread);
                Kernel32.DebugSetProcessKillOnExit(true);

                this.ignoreBreakpoint = true;

                this.DebuggeePid = (uint)procInfo.dwProcessId;
                this.DebuggerThreadProcReal();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
        }

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

                this.DebuggeePid = pid;
                this.DebuggerThreadProcReal();
            }
            catch (Exception ex)
            {
                this.HandleDebuggerException(ex);
            }
        }

        private void DebuggerThreadProcReal()
        {
            int debugEventBufferLength = Marshal.SizeOf(typeof(DEBUG_EVENT)) + Marshal.SizeOf(typeof(EXCEPTION_DEBUG_INFO));
            IntPtr debugEventBuffer = Marshal.AllocHGlobal(debugEventBufferLength);
            IntPtr debugEventUnion = debugEventBuffer + 12;
            try
            {
                for (; ; )
                {
                    if (this.isStopping)
                        break;
                    if (this.debuggeeExited)
                        break;

                    Kernel32.ZeroMemory(debugEventBuffer, (IntPtr)debugEventBufferLength);
                    bool ret = Kernel32.WaitForDebugEvent(debugEventBuffer, 0xFFFFFFFF);
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
            this.logger.Debug("Debugger thread exited.");
        }

        #region Debug event handlers

        private bool OnExceptionDebugEvent(uint pid, uint tid, EXCEPTION_DEBUG_INFO info)
        {
            ExceptionDebugInfo marshaledInfo = this.MarshalDebugInfo(info.ExceptionRecord);
            bool isFirstChance = info.dwFirstChance == 1;

            this.logger.Info(String.Format("Exception in thread {0} ({1}-chance):\r\n{2}",
                tid,
                isFirstChance ? "first" : "second",
                marshaledInfo.ToString()));

            if (this.ignoreBreakpoint && marshaledInfo.ExceptionCode == EXCEPTION_CODE.EXCEPTION_BREAKPOINT)
            {
                this.ignoreBreakpoint = false;
                return true;
            }

            if (this.ExceptionEvent != null)
            {
                ExceptionEventParams e = new ExceptionEventParams();
                e.Info = marshaledInfo;
                e.IsFirstChance = isFirstChance;
                this.ExceptionEvent(this, e);
            }

            return isFirstChance;
        }

        private bool OnCreateThreadDebugEvent(uint pid, uint tid, CREATE_THREAD_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("CreateThread event: pid {0}, tid {1}", pid, tid));

            // Track the thread.
            this.AddThread(tid, info.hThread);

            return true;
        }

        private bool OnCreateProcessDebugEvent(uint pid, uint tid, CREATE_PROCESS_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("CreateProcess event: pid {0}, tid {1}", pid, tid));

            // Track the thread.
            this.AddThread(tid, info.hThread);

            // Locate the process PEB. Very hacky.
            ThreadInfo threadInfo = this.threadMap[tid];
            this.targetProcessPebAddress = (IntPtr)DebuggerHelper.ReadTargetStructure(
                this.targetProcessHandle,
                threadInfo.TebLinearAddress + 0x30,
                typeof(IntPtr));

            // Close the image handle.
            Kernel32.CloseHandle(info.hFile);

            return true;
        }

        private bool OnExitThreadDebugEvent(uint pid, uint tid, EXIT_THREAD_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("ExitThread event: pid {0}, tid {1}", pid, tid));

            this.threadMap.Remove(tid);

            return true;
        }

        private bool OnExitProcessDebugEvent(uint pid, uint tid, EXIT_PROCESS_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("ExitProcess event: pid {0}, tid {1}", pid, tid));

            this.threadMap.Remove(tid);

            if (this.DebuggeePid == pid)
            {
                this.debuggeeExited = true;
            }

            return true;
        }

        private bool OnLoadDllDebugEvent(uint pid, uint tid, LOAD_DLL_DEBUG_INFO info)
        {
            string imageName = "unknown";
            this.UpdateModuleList();

            // NOTE: this is not a reliable way.
            if (info.lpImageName != IntPtr.Zero)
            {
                byte[] data = new byte[IntPtr.Size];
                uint bytesRead = 0;
                Kernel32.ReadProcessMemory(
                    this.targetProcessHandle,
                    info.lpImageName,
                    data,
                    (uint)IntPtr.Size,
                    out bytesRead);
                IntPtr lpImageName = (IntPtr)BitConverter.ToUInt32(data, 0);
                if (lpImageName != IntPtr.Zero)
                {
                    if (info.fUnicode != 0)
                    {
                        imageName = DebuggerHelper.ReadNullTerminatedStringUnicode(this.targetProcessHandle, lpImageName);
                    }
                    else
                    {
                        imageName = DebuggerHelper.ReadNullTerminatedStringAscii(this.targetProcessHandle, lpImageName);
                    }
                }
            }

            this.logger.Debug(String.Format("LoadDll event: pid {0}, tid {1}: {2}", pid, tid, imageName));

            if (this.SharedLibraryLoadedEvent != null)
            {
                SharedLibraryLoadedEventParams e = new SharedLibraryLoadedEventParams();
                e.ImageBase = info.lpBaseOfDll;
                e.ImageName = imageName;
                this.SharedLibraryLoadedEvent(this, e);
            }

            Kernel32.CloseHandle(info.hFile);
            return true;
        }

        private bool OnUnloadDllDebugEvent(uint pid, uint tid, UNLOAD_DLL_DEBUG_INFO info)
        {
            this.logger.Debug(String.Format("UnloadDll event: pid {0}, tid {1}: {2}", pid, tid));

            if (this.SharedLibraryUnloadedEvent != null)
            {
                SharedLibraryUnloadedEventParams e = new SharedLibraryUnloadedEventParams();
                e.ImageBase = info.lpBaseOfDll;
                this.SharedLibraryUnloadedEvent(this, e);
            }

            return true;
        }

        private bool OnOutputDebugStringDebugEvent(uint pid, uint tid, OUTPUT_DEBUG_STRING_INFO info)
        {
            // TODO: Handle?
            return true;
        }

        private bool OnRipDebugEvent(uint pid, uint tid, RIP_INFO info)
        {
            // TODO: Handle?
            return true;
        }

        #endregion

        private void AddThread(uint tid, IntPtr handle)
        {
            ThreadInfo info = new ThreadInfo();

            CONTEXT threadContext = this.GetThreadContext(handle);
            LDT_ENTRY tebLdtEntry = GetThreadLdtEntry(handle, threadContext.SegFs);

            info.Handle = handle;
            info.TebLinearAddress = (IntPtr)tebLdtEntry.Base;

            this.threadMap[tid] = info;
        }

        private void UpdateModuleList()
        {
            PEB peb = (PEB)DebuggerHelper.ReadTargetStructure(
                this.targetProcessHandle,
                this.targetProcessPebAddress,
                typeof(PEB));
            if (peb.LoaderData == IntPtr.Zero)
            {
                return;
            }

            PEB_LDR_DATA pebLoaderData = (PEB_LDR_DATA)DebuggerHelper.ReadTargetStructure(
                this.targetProcessHandle,
                peb.LoaderData,
                typeof(PEB_LDR_DATA));
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
                        break;
                    }
                default:
                    obj = new ExceptionDebugInfo();
                    break;
            }

            obj.ExceptionCode = info.ExceptionCode;
            obj.ExceptionAddress = info.ExceptionAddress;
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
