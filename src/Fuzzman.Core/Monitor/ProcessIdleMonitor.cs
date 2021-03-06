﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Monitor
{
    public class ProcessIdleMonitor : IProcessMonitor
    {
        public ProcessIdleMonitor()
        {
            this.ProcessId = 0;
            this.PollInterval = 100;
            this.MaxIdleCount = 5;
            this.CheckTimes = false;
            this.CheckContextSwitches = false;
        }

        public ProcessIdleMonitor(ProcessIdleMonitorConfig config)
        {
            this.ProcessId = 0;
            this.PollInterval = config.PollInterval;
            this.MaxIdleCount = config.MaxIdleCount;
            this.CheckTimes = config.CheckTimes;
            this.CheckContextSwitches = config.CheckContextSwitches;
        }

        public uint ProcessId { get; private set; }

        public int PollInterval { get; set; }

        public int MaxIdleCount { get; set; }

        public bool CheckTimes { get; set; }

        public bool CheckContextSwitches { get; set; }

        public event KillTargetEventHandler KillTargetEvent;

        public void Start()
        {
            this.isStopping = false;
            pollThread = new Thread(this.PollThread);
            pollThread.Name = "ProcessIdleMonitor Polling Thread";
            pollThread.Priority = ThreadPriority.Highest;
            pollThread.Start();
        }

        public void Attach(uint pid)
        {
            this.ProcessId = pid;
            this.killTargetFired = false;
        }

        public void Detach()
        {
            this.ProcessId = 0;
        }

        public void Stop()
        {
            this.isStopping = true;
            if (this.pollThread != null && this.pollThread.IsAlive)
            {
                this.pollThread.Join();
            }
            this.pollThread = null;
        }

        private Thread pollThread;
        private bool isStopping;
        private bool killTargetFired;

        private void PollThread()
        {
            const UInt64 timeDeltaThreshold = 50;
            const UInt64 contextSwitchesThreshold = 1;

            UInt64 kernelTime = 0;
            UInt64 userTime = 0;
            UInt64 contextSwitches = 0;
            int inactivityCounter = 0;

            while (!this.isStopping)
            {
                Thread.Sleep(this.PollInterval);

                if (this.ProcessId == 0)
                {
                    kernelTime = 0;
                    userTime = 0;
                    contextSwitches = 0;
                    inactivityCounter = 0;
                    continue;
                }

                IntPtr psi = IntPtr.Zero;
                int psiLength = 0x1000;

                for (; ; )
                {
                    psi = Marshal.AllocHGlobal(psiLength);

                    IntPtr fakePsiLength = (IntPtr)psiLength;
                    IntPtr returnLength;
                    NTSTATUS status = Ntdll.NtQuerySystemInformation(
                        SystemInformationClass.SystemProcessInformation,
                        psi,
                        fakePsiLength,
                        out returnLength);

                    if (status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
                    {
                        Marshal.FreeHGlobal(psi);
                        psiLength *= 2;
                        continue;
                    }
                    if (status == NTSTATUS.STATUS_SUCCESS)
                    {
                        break;
                    }

                    Marshal.FreeHGlobal(psi);
                    return;
                }

                IntPtr current = psi;
                SYSTEM_PROCESS_INFORMATION processInfo;
                do
                {
                    processInfo = (SYSTEM_PROCESS_INFORMATION)Marshal.PtrToStructure(current, typeof(SYSTEM_PROCESS_INFORMATION));

                    if (processInfo.ProcessId == this.ProcessId)
                    {
                        bool inactive = false;

                        if (this.CheckTimes)
                        {
                            UInt64 kernelTimeDelta = processInfo.KernelTime - kernelTime;
                            UInt64 userTimeDelta = processInfo.UserTime - userTime;
                            if (kernelTimeDelta < timeDeltaThreshold && userTimeDelta < timeDeltaThreshold)
                            {
                                inactive = true;
                            }
                            kernelTime = processInfo.KernelTime;
                            userTime = processInfo.UserTime;
                        }

                        if (this.CheckContextSwitches)
                        {
                            UInt64 contextSwitchesUpdated = 0;
                            IntPtr threadPtr = current + Marshal.SizeOf(typeof(SYSTEM_PROCESS_INFORMATION));
                            for (int threadIndex = 0; threadIndex < processInfo.NumberOfThreads; ++threadIndex)
                            {
                                SYSTEM_THREAD threadInfo = (SYSTEM_THREAD)Marshal.PtrToStructure(threadPtr, typeof(SYSTEM_THREAD));
                                contextSwitchesUpdated += threadInfo.ContextSwitchCount;
                                threadPtr += Marshal.SizeOf(typeof(SYSTEM_THREAD));
                            }

                            UInt64 contextSwitchesDelta = contextSwitchesUpdated - contextSwitches;
                            if (contextSwitchesDelta < contextSwitchesThreshold)
                            {
                                inactive = true;
                            }

                            contextSwitches = contextSwitchesUpdated;
                        }

                        inactivityCounter = inactive ? inactivityCounter + 1 : 0;
                        if (!this.killTargetFired && inactivityCounter >= this.MaxIdleCount)
                        {
                            this.killTargetFired = true;
                            if (this.KillTargetEvent != null)
                            {
                                this.KillTargetEvent();
                            }
                        }
                        break;
                    }

                    current += (int)processInfo.NextEntryOffset;
                } while (processInfo.NextEntryOffset != 0);

                Marshal.FreeHGlobal(psi);
            }
        }
    }
}
