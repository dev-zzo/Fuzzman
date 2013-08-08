using System;
using System.Runtime.Serialization;
using Fuzzman.Core.Interop;

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
            : base(String.Format("{0}\r\nWin32 error code: {1} ({2:X8})", message, Kernel32Helpers.GetSystemMessage(errorCode), errorCode))
        {
        }

        protected DebuggerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
