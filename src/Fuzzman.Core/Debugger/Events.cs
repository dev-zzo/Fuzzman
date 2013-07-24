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

    public class ProcessCreatedEventParams
    {
        public uint ProcessId;
    }

    public class ThreadCreatedEventParams
    {
        public uint ProcessId;
        public uint ThreadId;
    }

    public class ThreadExitedEventParams
    {
        public uint ProcessId;
        public uint ThreadId;
        public uint ExitCode;
    }

    public class ProcessExitedEventParams
    {
        public uint ProcessId;
        public uint ExitCode;
    }

    public delegate void ExceptionEventHandler(IDebugger sender, ExceptionEventParams info);

    public delegate void SharedLibraryLoadedEventHandler(IDebugger sender, SharedLibraryLoadedEventParams e);

    public delegate void SharedLibraryUnloadedEventHandler(IDebugger sender, SharedLibraryUnloadedEventParams e);

    public delegate void ProcessCreatedEventHandler(IDebugger sender, ProcessCreatedEventParams e);

    public delegate void ThreadCreatedEventHandler(IDebugger sender, ThreadCreatedEventParams e);

    public delegate void ThreadExitedEventHandler(IDebugger sender, ThreadExitedEventParams e);

    public delegate void ProcessExitedEventHandler(IDebugger sender, ProcessExitedEventParams e);
}
