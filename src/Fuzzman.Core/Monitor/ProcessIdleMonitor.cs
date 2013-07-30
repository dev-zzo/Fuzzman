using System;
using System.Threading;
using Fuzzman.Core.Interop;

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
        }

        public int PollInterval { get; set; }

        public int MaxIdleCount { get; set; }

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
            IntPtr processHandle = Kernel32.OpenProcess(ProcessAccess.QueryInformation, false, this.processId);
            if (processHandle == IntPtr.Zero)
            {
                return;
            }

            UInt64 kernelTimePrevious = 0;
            UInt64 kernelTimeUpdated;
            UInt64 userTimePrevious = 0;
            UInt64 userTimeUpdated;
            UInt64 timeDeltaThreshold = 50;
            int inactivityCounter = 0;

            while (!this.isStopping)
            {
                Thread.Sleep(this.PollInterval);

                UInt64 dummy;
                Kernel32.GetProcessTimes(
                    processHandle,
                    out dummy,
                    out dummy,
                    out kernelTimeUpdated,
                    out userTimeUpdated);

                UInt64 kernelTimeDelta = kernelTimeUpdated - kernelTimePrevious;
                UInt64 userTimeDelta = userTimeUpdated - userTimePrevious;

                if (kernelTimeDelta < timeDeltaThreshold && userTimeDelta < timeDeltaThreshold)
                {
                    inactivityCounter++;

                    if (inactivityCounter >= this.MaxIdleCount)
                    {
                        this.isStopping = true;
                        if (this.IdleEvent != null)
                        {
                            this.IdleEvent();
                        }
                    }
                }
                else
                {
                    inactivityCounter = 0;
                }

                kernelTimePrevious = kernelTimeUpdated;
                userTimePrevious = userTimeUpdated;
            }

            if (processHandle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(processHandle);
            }
        }
    }
}
