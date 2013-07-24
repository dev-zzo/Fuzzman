using System;
using System.Collections.Generic;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core
{
    public interface IDebugger
    {
        uint DebuggeePid { get; }


        IDictionary<uint, ThreadInfo> Threads { get; }

        IDictionary<IntPtr, ModuleInfo> Modules { get; }


        event ExceptionEventHandler ExceptionEvent;

        event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;


        void StartTarget(string commandLine);

        void AttachToTarget(uint pid);

        void TerminateTarget();

        CONTEXT GetThreadContext(uint tid);

        LDT_ENTRY GetThreadLdtEntry(uint tid, uint selector);
    }
}
