using System;
using Fuzzman.Core.Interop;

namespace Fuzzman.Agent
{
    public class FaultReport
    {
        public int TimesOccurred;

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }
    }

    public class ExceptionFaultReport : FaultReport
    {
        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public string Location;

        public CONTEXT Context;

        public override int GetHashCode()
        {
            int hash = (int)this.ExceptionCode;
            hash ^= this.Location != null ? this.Location.GetHashCode() : (int)this.OffendingVA;
            return hash;
        }
    }

    public class AccessViolationFaultReport : ExceptionFaultReport
    {
        public string AccessType;

        public IntPtr TargetVA;

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ this.AccessType.GetHashCode();
        }
    }
}
