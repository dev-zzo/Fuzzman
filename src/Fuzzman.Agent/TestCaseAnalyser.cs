using System;
using System.Collections.Generic;
using System.Text;

namespace Fuzzman.Agent
{
    public class TestCaseAnalyser
    {
        public TestCaseAnalyser(TestCase testCase)
        {
            this.testCase = testCase;
            this.IsInteresting = false;
        }

        public bool IsInteresting { get; private set; }

        public string ReportSummary { get; private set; }

        public string ReportText { get { return this.reportBuilder.ToString(); } }

        public void Analyse()
        {
            if (this.testCase.Reports.Count == 0)
            {
                return;
            }

            this.IsInteresting = true;
            this.reportBuilder.AppendFormat("* * * FAULT ANALYSIS START * * *\r\n\r\n");

            this.SortFaultReports();
            this.BuildSummary(mergedReports);
            this.reportBuilder.AppendFormat("Detailed analysis of each fault follows.\r\n\r\n");
            int counter = 1;
            foreach (AccessViolationFaultReport avfr in this.accessViolations)
            {
                this.reportBuilder.AppendFormat("* * * FAULT #{0} (encountered {1} time(s))\r\n\r\n", counter++, avfr.OccurrenceCount);
                this.AnalyseFaultReport(avfr);
            }
            foreach (ExceptionFaultReport efr in this.genericExceptions)
            {
                this.reportBuilder.AppendFormat("* * * FAULT #{0} (encountered {1} time(s))\r\n\r\n", counter++, efr.OccurrenceCount);
                this.AnalyseFaultReport(efr);
            }

            this.reportBuilder.AppendFormat("\r\n* * * FAULT ANALYSIS END * * *\r\n");
        }

        private readonly TestCase testCase;
        private readonly StringBuilder reportBuilder = new StringBuilder(4096);
        private readonly List<FaultReport> mergedReports = new List<FaultReport>();
        private readonly List<AccessViolationFaultReport> accessViolations = new List<AccessViolationFaultReport>();
        private readonly List<ExceptionFaultReport> genericExceptions = new List<ExceptionFaultReport>();

        private void SortFaultReports()
        {
            // Merge similar reports into one.
            foreach (FaultReport report in this.testCase.Reports)
            {
                bool found = false;
                foreach (FaultReport otherReport in this.mergedReports)
                {
                    if (report.Equals(otherReport))
                    {
                        found = true;
                        otherReport.OccurrenceCount++;
                        break;
                    }
                }
                if (!found)
                {
                    this.mergedReports.Add(report);
                }
            }

            // Separate AVs, exceptions, and other faults.
            foreach (FaultReport report in this.mergedReports)
            {
                AccessViolationFaultReport avfr = report as AccessViolationFaultReport;
                if (avfr != null)
                {
                    this.accessViolations.Add(avfr);
                    continue;
                }

                ExceptionFaultReport efr = report as ExceptionFaultReport;
                if (efr != null)
                {
                    this.genericExceptions.Add(efr);
                    continue;
                }
            }

            this.reportBuilder.AppendFormat("{0} AV case(s), {1} other exception(s).\r\n",
                this.accessViolations.Count,
                this.genericExceptions.Count);
        }

        private void BuildSummary(List<FaultReport> mergedReports)
        {
            if (mergedReports.Count == 1)
            {
                FaultReport report = mergedReports[0];
                AccessViolationFaultReport avfr = report as AccessViolationFaultReport;
                if (avfr != null)
                {
                    if ((uint)avfr.TargetVA == avfr.Context.Eip)
                    {
                        this.ReportSummary = String.Format("AV_X_{0:X8}",
                            (uint)avfr.TargetVA);
                        return;
                    }

                    string location = avfr.Location != "???" ? avfr.Location : avfr.OffendingVA.ToString("X8");
                    this.ReportSummary = String.Format("AV_{0}_{1}_{2:X8}",
                        avfr.AccessType[0],
                        location,
                        (uint)avfr.TargetVA);
                    return;
                }

                ExceptionFaultReport efr = report as ExceptionFaultReport;
                if (efr != null)
                {
                    string location = avfr.Location != "???" ? avfr.Location : avfr.OffendingVA.ToString("X8");
                    this.ReportSummary = String.Format("EX_{0:X8}_{1}",
                        (uint)efr.ExceptionCode,
                        location);
                    return;
                }

                this.ReportSummary = "UNKNOWN";
            }
            else
            {
                this.ReportSummary = "UNSTABLE";
            }
        }

        private void AnalyseFaultReport(AccessViolationFaultReport report)
        {
            this.reportBuilder.AppendFormat("Access violation ({0}) at {1:X8} -> {2:X8}\r\n",
                report.AccessType,
                (uint)report.OffendingVA,
                (uint)report.TargetVA);
            this.reportBuilder.AppendLine("Register dump:");
            this.reportBuilder.AppendLine(report.Context.ToString());
            this.reportBuilder.AppendLine();
            this.reportBuilder.AppendLine();
        }

        private void AnalyseFaultReport(ExceptionFaultReport report)
        {
            this.reportBuilder.AppendFormat("Exception {0} ({1:X8}) at {2:X8}\r\n",
                report.ExceptionCode,
                (uint)report.ExceptionCode,
                (uint)report.OffendingVA);
            this.reportBuilder.AppendLine("Register dump:");
            this.reportBuilder.AppendLine(report.Context.ToString());
            this.reportBuilder.AppendLine();
            this.reportBuilder.AppendLine();
        }

        private void AnalyseFaultReport(FaultReport report)
        {
        }
    }
}
