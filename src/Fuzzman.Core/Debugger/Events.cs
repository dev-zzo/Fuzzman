using System;
using Fuzzman.Core.Interop;
using Fuzzman.Core.Debugger.DebugInfo;

namespace Fuzzman.Core.Debugger
{
    public class ExceptionEventParams
    {
        public ExceptionDebugInfo Info;
        public bool IsFirstChance;
    }

    public class SharedLibraryLoadedEventParams
    {
        public IntPtr ImageBase;
        public string ImageName;
    }

    public class SharedLibraryUnloadedEventParams
    {
        public IntPtr ImageBase;
    }

    public delegate void ExceptionEventHandler(IDebugger sender, ExceptionEventParams info);

    public delegate void SharedLibraryLoadedEventHandler(IDebugger sender, SharedLibraryLoadedEventParams e);

    public delegate void SharedLibraryUnloadedEventHandler(IDebugger sender, SharedLibraryUnloadedEventParams e);
}
