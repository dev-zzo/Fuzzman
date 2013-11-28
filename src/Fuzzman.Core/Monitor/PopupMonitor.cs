using System;
using System.Text;
using System.Threading;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Monitor
{
    /// <summary>
    /// Monitor for windows that match the given title, and close them.
    /// </summary>
    public class PopupMonitor : IGlobalMonitor
    {
        public PopupMonitor(PopupMonitorConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            this.timer = new Timer(this.TimerCallback, null, 500, 500);
        }

        public void Stop()
        {
            this.timer.Dispose();
            this.timer = null;
        }

        private readonly PopupMonitorConfig config;
        private Timer timer;

        private void TimerCallback(object state)
        {
            User32.EnumWindows(this.EnumCallback, IntPtr.Zero);
        }

        private bool EnumCallback(IntPtr hWnd, IntPtr lParam)
        {
            // Really hacky, but works for now.
            StringBuilder windowText = new StringBuilder(256);
            User32.GetWindowText(hWnd, windowText, 256);
            if (windowText.ToString() == this.config.WatchFor)
            {
                StringBuilder windowClass = new StringBuilder(256);
                User32.GetClassName(hWnd, windowClass, 256);
                if (windowClass.ToString() == "#32770")
                {
                    User32.PostMessage(hWnd, (uint)WM.SYSCOMMAND, (IntPtr)SC.CLOSE, IntPtr.Zero);
                }
            }
            return true;
        }
    }
}
