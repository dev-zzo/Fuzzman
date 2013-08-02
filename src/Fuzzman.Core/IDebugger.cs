using System;
using System.Collections.Generic;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core
{
    public interface IDebugger : IDisposable
    {
        uint DebuggeePid { get; }

        bool IsRunning { get; }


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

        void Stop();
    }
}
