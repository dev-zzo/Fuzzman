using System;
using System.Text;
using Fuzzman.Core.Interop;

namespace Fuzzman.Core.Debugger.DebugInfo
{
    public class ExceptionDebugInfo
    {
        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public bool IsContinuable;

        public ExceptionDebugInfo NestedException;
    }

    public class AccessViolationDebugInfo : ExceptionDebugInfo
    {
        public enum AccessType
        {
            Read = 0,
            Write = 1,
            DEP = 8,
        }

        public AccessType Type;

        public IntPtr TargetVA;
    }
}
