using System;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Debugger.Simple;
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
        public EXCEPTION_CODE ExceptionCode;

        public IntPtr OffendingVA;

        public Location Location;

        public CONTEXT Context;

        public uint[] StackDump;

        public virtual void Fill(IDebugger debugger, ThreadInfo threadInfo, ProcessInfo processInfo, ExceptionDebugInfo debugInfo, ExceptionFaultReport report)
        {
            this.ExceptionCode = debugInfo.ExceptionCode;
            this.OffendingVA = debugInfo.OffendingVA;

            this.Location = DebuggerHelper.LocateModuleOffset(processInfo.Handle, processInfo.PebLinearAddress, debugInfo.OffendingVA);

            this.Context = new CONTEXT();
            this.Context.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
            Kernel32.GetThreadContext(threadInfo.Handle, ref this.Context); // Might fail.

            // TODO: Bitness warning.
            int stackTraceCount = 32;
            int stackTraceSize = stackTraceCount * 4;
            byte[] rawStack = new byte[stackTraceSize];
            uint bytesRead;
            if (Kernel32.ReadProcessMemory(processInfo.Handle, (IntPtr)this.Context.Esp, rawStack, (uint)stackTraceSize, out bytesRead))
            {
                // Make sure we process only as many bytes as were actually read.
                stackTraceSize = (int)bytesRead;
                stackTraceCount = stackTraceSize / 4;

                this.StackDump = new uint[stackTraceCount];
                for (int i = 0; i < stackTraceCount; ++i)
                {
                    this.StackDump[i] = BitConverter.ToUInt32(rawStack, i * 4);
                }
            }
        }

        public override bool Equals(object obj)
        {
            ExceptionFaultReport efr = obj as ExceptionFaultReport;
            if (efr == null)
                return false;
            if (this.ExceptionCode != efr.ExceptionCode)
                return false;
            if (this.Location != null && !this.Location.Equals(efr.Location))
                return false;
            if (this.Location == null && this.OffendingVA != efr.OffendingVA)
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

        public override void Fill(IDebugger debugger, ThreadInfo threadInfo, ProcessInfo processInfo, ExceptionDebugInfo debugInfo, ExceptionFaultReport report)
        {
            base.Fill(debugger, threadInfo, processInfo, debugInfo, report);
            AccessViolationDebugInfo avDebugInfo = debugInfo as AccessViolationDebugInfo;
            this.AccessType = avDebugInfo.Type.ToString();
            this.TargetVA = avDebugInfo.TargetVA;
        }

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
