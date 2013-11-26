using System;
using System.Threading;

namespace Fuzzman.Core.Monitor
{
    public class TimeoutMonitor : IMonitor
    {
        public TimeoutMonitor(TimeoutMonitorConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            this.timer = new Timer(this.TimerCallback, null, this.config.Interval * 1000, Timeout.Infinite);
        }

        public void Attach(uint pid)
        {
        }

        public void Detach()
        {
            this.Stop();
        }

        public void Stop()
        {
            this.timer.Dispose();
            this.timer = null;
        }

        public event KillTargetEventHandler KillTargetEvent;

        private readonly TimeoutMonitorConfig config;
        private Timer timer;

        private void TimerCallback(object state)
        {
            if (this.KillTargetEvent != null)
            {
                this.KillTargetEvent();
                this.Stop();
            }
        }
    }
}
