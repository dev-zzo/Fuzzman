using System;
using System.Threading;
using Fuzzman.Core.Interop;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Monitor
{
    public delegate void ProcessIdleEventHandler();

    public class ProcessIdleMonitor
    {
        public ProcessIdleMonitor(uint processId)
        {
            this.processId = processId;
            this.PollInterval = 100;
            this.MaxIdleCount = 5;
            this.CheckTimes = false;
            this.CheckContextSwitches = false;
        }

        public int PollInterval { get; set; }

        public int MaxIdleCount { get; set; }

        public bool CheckTimes { get; set; }

        public bool CheckContextSwitches { get; set; }

        public event ProcessIdleEventHandler IdleEvent;

        public void Start()
        {
            pollThread = new Thread(this.PollThread);
            pollThread.Priority = ThreadPriority.Highest;
            pollThread.Start();
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

        private uint processId;
        private Thread pollThread;
        private bool isStopping = false;

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

                    if (processInfo.ProcessId == this.processId)
                    {
                        UInt64 kernelTimeDelta = processInfo.KernelTime - kernelTime;
                        UInt64 userTimeDelta = processInfo.UserTime - userTime;

                        bool inactive = true;

                        if (this.CheckTimes)
                        {
                            if (kernelTimeDelta >= timeDeltaThreshold || userTimeDelta >= timeDeltaThreshold)
                            {
                                inactive = false;
                            }
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
                            if (contextSwitchesDelta >= contextSwitchesThreshold)
                            {
                                inactive = false;
                            }

                            contextSwitches = contextSwitchesUpdated;
                        }

                        inactivityCounter = inactive ? inactivityCounter + 1 : 0;
                        if (inactivityCounter >= this.MaxIdleCount)
                        {
                            this.isStopping = true;
                            if (this.IdleEvent != null)
                            {
                                this.IdleEvent();
                            }
                        }

                        kernelTime = processInfo.KernelTime;
                        userTime = processInfo.UserTime;

                        break;
                    }

                    current += (int)processInfo.NextEntryOffset;
                } while (processInfo.NextEntryOffset != 0);

                Marshal.FreeHGlobal(psi);
            }
        }
    }
}
