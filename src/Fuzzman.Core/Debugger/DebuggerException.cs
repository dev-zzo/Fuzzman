using System;
using System.Runtime.Serialization;

namespace Fuzzman.Core.Debugger
{
    [Serializable]
    public class DebuggerException : Exception
    {
        public DebuggerException()
        {
        }
        
        public DebuggerException(string message)
            : base(message)
        {
        }

        public DebuggerException(string message, int errorCode)
            : base(message)
        {
        }

        protected DebuggerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
