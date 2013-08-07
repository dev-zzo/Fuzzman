using System;
using System.Collections.Generic;
using Fuzzman.Core.Debugger;

namespace Fuzzman.Core
{
    /// <summary>
    /// Simple debugger interface.
    /// NOTE: The debuggers usually do not have internal worker threads.
    /// </summary>
    public interface IDebugger : IDisposable
    {
        ProcessInfo Process { get; }

        IDictionary<uint, ThreadInfo> Threads { get; }


        event ExceptionEventHandler ExceptionEvent;

        event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;

        event ProcessCreatedEventHandler ProcessCreatedEvent;

        event ThreadCreatedEventHandler ThreadCreatedEvent;

        event ThreadExitedEventHandler ThreadExitedEvent;

        event ProcessExitedEventHandler ProcessExitedEvent;


        void CreateTarget(string commandLine);

        void AttachToTarget(uint pid);

        void TerminateTarget();

        void WaitAndDispatchEvent();
    }
}
