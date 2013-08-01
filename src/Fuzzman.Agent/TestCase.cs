using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fuzzman.Core;

namespace Fuzzman.Agent
{
    public class TestCase
    {
        public TestCase(int testCaseNumber)
        {
            this.TestCaseNumber = testCaseNumber;
        }

        public int TestCaseNumber { get; set; }

        public string TestCaseTemplate { get; set; }

        /// <summary>
        /// Temporary directory for the files generated.
        /// </summary>
        public string WorkingDirectory { get; set; }

        public string SaveDirectory { get; set; }

        /// <summary>
        /// Where the source file for this test case is located.
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// How to run the target application.
        /// </summary>
        public string CommandLineTemplate { get; set; }

        /// <summary>
        /// Reports associated with this test case.
        /// </summary>
        public IList<FaultReport> Reports { get { return this.reports; } }

        public void Setup(IFuzzer fuzzer)
        {
            if (Directory.Exists(this.WorkingDirectory))
            {
                Directory.Delete(this.WorkingDirectory, true);
            }
            Directory.CreateDirectory(this.WorkingDirectory);

            string sampleFileName = Path.GetFileName(this.SourceFilePath);
            this.samplePath = Path.Combine(this.WorkingDirectory, sampleFileName);
            File.Copy(this.SourceFilePath, this.samplePath);

            fuzzer.Process(this.samplePath);
        }

        public void Start(IDebugger debugger)
        {
            string cmdLine = this.MakeTestCommandLine();
            debugger.CreateTarget(cmdLine);
        }

        public void SaveResults()
        {
            int counter = 1;
            foreach (FaultReport report in this.Reports)
            {
                report.Generate(Path.Combine(this.WorkingDirectory, String.Format("fault-report-{0}.txt", counter)));
                counter++;
            }

            Directory.Move(this.WorkingDirectory, this.MakeTestCaseDir());
        }

        public void Cleanup()
        {
            if (Directory.Exists(this.WorkingDirectory))
            {
                Directory.Delete(this.WorkingDirectory, true);
            }
        }

        private readonly IList<FaultReport> reports = new List<FaultReport>();
        private string samplePath;

        private string MakeTestCommandLine()
        {
            StringBuilder builder = new StringBuilder(this.CommandLineTemplate, 256);

            builder.Replace("{TARGET}", this.samplePath);

            return builder.ToString();
        }

        private string MakeTestCaseDir()
        {
            StringBuilder builder = new StringBuilder(this.TestCaseTemplate, 256);

            builder.Replace("{TCN}", this.TestCaseNumber.ToString("D8"));
            builder.Replace("{DATETIME}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            builder.Replace("{SUMMARY}", this.Reports[0].GetSummary());

            return Path.Combine(this.SaveDirectory, builder.ToString());
        }

    }
}
