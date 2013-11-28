using System;
using System.Threading;

namespace Fuzzman.Core.Monitor
{
    public class TimeoutMonitor : IProcessMonitor
    {
        public TimeoutMonitor(TimeoutMonitorConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            this.timer = new Timer(this.TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Attach(uint pid)
        {
            if (this.timer != null)
            {
                this.timer.Change(this.config.Interval * 1000, this.config.Interval * 1000);
            }
        }

        public void Detach()
        {
            //if (this.timer != null)
            //{
            //    this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            //}
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
            }
        }
    }
}
