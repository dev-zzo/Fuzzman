using System;

namespace Fuzzman.Core.Debugger
{
    public class DebuggerSharedLibraryLoadedEventParams
    {
        public IntPtr ImageBase;
        public string ImageName;
    }

    public delegate void DebuggerSharedLibraryLoadedEventHandler(IDebugger sender, DebuggerSharedLibraryLoadedEventParams e);

    public class DebuggerSharedLibraryUnloadedEventParams
    {
        public IntPtr ImageBase;
    }

    public delegate void DebuggerSharedLibraryUnloadedEventHandler(IDebugger sender, DebuggerSharedLibraryUnloadedEventParams e);
}
