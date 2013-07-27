using System;
using System.IO;
using Fuzzman.Core.Interop;

namespace Fuzzman.Agent
{
    public class FaultReport
    {
        public virtual string GetSummary()
        {
            return "UNKNOWN";
        }

        public virtual void Generate(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("**** FAULT REPORT ****");
                writer.WriteLine();
            }
        }
    }

    public class ExceptionFaultReport : FaultReport
    {
        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public string RegisterDump;

        public override string GetSummary()
        {
            return String.Format("EXCEPTION_{0:X8}_{1:X8}", this.ExceptionCode, this.OffendingVA);
        }

        public override void Generate(string path)
        {
            base.Generate(path);
            using (FileStream stream = new FileStream(path, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("Registers:\r\n{0}", this.RegisterDump);
                writer.WriteLine("Fault type: uncaught exception");
                writer.WriteLine("Exception code: {0:X8} ({1})", (uint)this.ExceptionCode, this.ExceptionCode);
                writer.WriteLine("Exception addr: {0:X8}", (uint)this.OffendingVA);
            }
        }
    }

    public class AccessViolationFaultReport : ExceptionFaultReport
    {
        public string AccessType;

        public IntPtr TargetVA;

        public override string GetSummary()
        {
            return String.Format("AV_{0}_{1:X8}_{2:X8}", this.AccessType.ToUpper(), this.OffendingVA, this.TargetVA);
        }

        public override void Generate(string path)
        {
            base.Generate(path);
            using (FileStream stream = new FileStream(path, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("Access type: {0}", this.AccessType);
                writer.WriteLine("Target addr: {0:X8}", (uint)this.TargetVA);
            }
        }
    }
}
