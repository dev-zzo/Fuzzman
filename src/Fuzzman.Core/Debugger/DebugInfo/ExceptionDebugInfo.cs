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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Exception code: {0:X8} ({1})\r\n", (uint)this.ExceptionCode, this.ExceptionCode);
            builder.AppendFormat("Exception addr: {0:X16}\r\n", (UInt64)this.OffendingVA);
            builder.Append(this.GetSpecificInfo());
            if (this.NestedException != null)
            {
                builder.Append(this.NestedException.ToString());
            }
            return builder.ToString();
        }

        protected virtual string GetSpecificInfo()
        {
            return "";
        }
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

        protected override string GetSpecificInfo()
        {
            return String.Format("Access type: {0}\r\n", this.Type);
        }
    }
}
