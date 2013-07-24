using System;
using Fuzzman.Core.Interop;
using Fuzzman.Core.Debugger.DebugInfo;

namespace Fuzzman.Core.Debugger
{
    public class ExceptionEventParams
    {
        public uint ProcessId;
        public uint ThreadId;
        public bool IsFirstChance;
        public ExceptionDebugInfo Info;
    }

    public class SharedLibraryLoadedEventParams
    {
        public uint ProcessId;
        public IntPtr ImageBase;
    }

    public class SharedLibraryUnloadedEventParams
    {
        public uint ProcessId;
        public IntPtr ImageBase;
    }

    public class ProcessExitEventParams
    {
        public uint ProcessId;
        public uint ExitCode;
    }

    public delegate void ExceptionEventHandler(IDebugger sender, ExceptionEventParams info);

    public delegate void SharedLibraryLoadedEventHandler(IDebugger sender, SharedLibraryLoadedEventParams e);

    public delegate void SharedLibraryUnloadedEventHandler(IDebugger sender, SharedLibraryUnloadedEventParams e);

    public delegate void ProcessExitEventHandler(IDebugger sender, ProcessExitEventParams e);
}
