using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fuzzman.Core.Debugger.Native
{
    public class NativeDebugger : IDebugger
    {
        public uint DebuggeePid
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsRunning
        {
            get { throw new NotImplementedException(); }
        }

        public IDictionary<uint, ThreadInfo> Threads
        {
            get { throw new NotImplementedException(); }
        }

        public event ExceptionEventHandler ExceptionEvent;

        public event SharedLibraryLoadedEventHandler SharedLibraryLoadedEvent;

        public event SharedLibraryUnloadedEventHandler SharedLibraryUnloadedEvent;

        public event ProcessCreatedEventHandler ProcessCreatedEvent;

        public event ThreadCreatedEventHandler ThreadCreatedEvent;

        public event ThreadExitedEventHandler ThreadExitedEvent;

        public event ProcessExitedEventHandler ProcessExitedEvent;

        public void CreateTarget(string commandLine)
        {
            throw new NotImplementedException();
        }

        public void AttachToTarget(uint pid)
        {
            throw new NotImplementedException();
        }

        public void TerminateTarget()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
