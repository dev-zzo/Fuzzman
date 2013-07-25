using System;
using System.Threading;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Monitor
{
    /// <summary>
    /// Report the CPU time usage.
    /// All times are reported as delta values referring to the previous event.
    /// </summary>
    /// <param name="kernelDelta">Kernel mode time usage, us.</param>
    /// <param name="userDelta">User mode time usage, us.</param>
    public delegate void CpuUsageMonitorEventHandler(UInt64 kernelDelta, UInt64 userDelta);

    /// <summary>
    /// Monitors the CPU usage times of a given process.
    /// </summary>
    public class CpuUsageMonitor
    {
        public CpuUsageMonitor(uint processId)
        {
            this.processId = processId;
        }

        public int PollInterval { get; set; }

        public event CpuUsageMonitorEventHandler MonitorEvent;

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

                if (this.MonitorEvent != null)
                {
                    this.MonitorEvent(kernelTimeDelta / 10, userTimeDelta / 10);
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
