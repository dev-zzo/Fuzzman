using System;
using Fuzzman.Core.Interop;

namespace Fuzzman.Agent
{
    public class FaultReport
    {
    }

    public class ExceptionFaultReport : FaultReport
    {
        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public string Location;

        public CONTEXT Context;
    }

    public class AccessViolationFaultReport : ExceptionFaultReport
    {
        public string AccessType;

        public IntPtr TargetVA;
    }
}
