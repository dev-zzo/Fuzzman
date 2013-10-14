using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Fuzzman.Core;

namespace Fuzzman.Agent
{
    public class TestCase
    {
        public TestCase()
        {
            this.TestCaseNumber = Interlocked.Increment(ref nextTestCaseNumber);
        }

        /// <summary>
        /// Unique test case ID in this test run.
        /// </summary>
        public int TestCaseNumber { get; private set; }

        /// <summary>
        /// How many times the test case has been run.
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Timestamp when the program-under-test has been started.
        /// </summary>
        public DateTime StartTime { get; set; }

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

        public string CommandLine { get; private set; }

        /// <summary>
        /// Reports associated with this test case.
        /// </summary>
        public IList<FaultReport> Reports { get { return this.reports; } }

        public void Setup(IFuzzer fuzzer)
        {
            this.Cleanup();
            Directory.CreateDirectory(this.WorkingDirectory);

            string sampleFileName = Path.GetFileName(this.SourceFilePath);
            string samplePath = Path.Combine(this.WorkingDirectory, sampleFileName);
            File.Copy(this.SourceFilePath, samplePath);

            fuzzer.Process(samplePath);

            StringBuilder builder = new StringBuilder(this.CommandLineTemplate, 256);
            builder.Replace("{TARGET}", samplePath);
            this.CommandLine = builder.ToString();
        }

        public void SaveResults(string summary, string details)
        {
            using (FileStream stream = new FileStream(Path.Combine(this.WorkingDirectory, "analysis.txt"), FileMode.CreateNew, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(details);
            }

            StringBuilder builder = new StringBuilder(this.TestCaseTemplate, 256);
            builder.Replace("{TCN}", this.TestCaseNumber.ToString("D8"));
            builder.Replace("{DATETIME}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            builder.Replace("{SUMMARY}", summary);

            int retryCount = 10;
            do
            {
                try
                {
                    Directory.Move(this.WorkingDirectory, Path.Combine(this.SaveDirectory, builder.ToString()));
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Bah! Failed to move the working directory!");
                    --retryCount;
                    Thread.Sleep(2500);
                }
            } while (retryCount > 0);
            if (retryCount == 0)
            {
                // Save the data, at least...
                this.WorkingDirectory = "";
            }
        }

        public void Cleanup()
        {
            if (Directory.Exists(this.WorkingDirectory))
            {
                int retryCount = 10;
                do
                {
                    try
                    {
                        Directory.Delete(this.WorkingDirectory, true);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Bah! Failed to delete the working directory!");
                        --retryCount;
                        Thread.Sleep(2500);
                    }
                } while (retryCount > 0);
            }
        }

        private static int nextTestCaseNumber = 0;
        private readonly IList<FaultReport> reports = new List<FaultReport>();
    }
}
