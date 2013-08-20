using System;

namespace Fuzzman.Core.Debugger
{
    public class ProcessInfo
    {
        public uint Pid;

        public string ImagePath;

        public IntPtr Handle;

        public IntPtr PebLinearAddress;
    }
}
