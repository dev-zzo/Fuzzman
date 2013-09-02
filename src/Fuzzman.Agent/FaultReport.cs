using System;
using Fuzzman.Core.Interop;

namespace Fuzzman.Agent
{
    public class FaultReport
    {
        public FaultReport()
        {
            this.OccurrenceCount = 1;
        }

        public int OccurrenceCount;
    }

    public class ExceptionFaultReport : FaultReport
    {
        public CONTEXT Context;

        public uint[] StackDump;

        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public string Location;

        public override bool Equals(object obj)
        {
            ExceptionFaultReport efr = obj as ExceptionFaultReport;
            if (efr == null)
                return false;
            if (this.ExceptionCode != efr.ExceptionCode)
                return false;
            if (this.Location != "???" && this.Location != efr.Location)
                return false;
            if (this.Location == "???" && this.OffendingVA != efr.OffendingVA)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return (int)this.ExceptionCode ^ this.Location.GetHashCode();
        }
    }

    public class AccessViolationFaultReport : ExceptionFaultReport
    {
        public string AccessType;

        public IntPtr TargetVA;

        public override bool Equals(object obj)
        {
            AccessViolationFaultReport avfr = obj as AccessViolationFaultReport;
            if (avfr == null)
                return false;
            if (this.AccessType != avfr.AccessType)
                return false;
            if (this.TargetVA != avfr.TargetVA)
                return false;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ this.AccessType.GetHashCode() ^ (int)this.TargetVA;
        }
    }
}
