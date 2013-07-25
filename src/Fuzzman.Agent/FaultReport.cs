using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fuzzman.Core.Interop;
using System.IO;

namespace Fuzzman.Agent
{
    public class FaultReport
    {
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

        public override void Generate(string path)
        {
            base.Generate(path);
            using (FileStream stream = new FileStream(path, FileMode.Append))
            using (StreamWriter writer = new StreamWriter(stream))
            {
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
