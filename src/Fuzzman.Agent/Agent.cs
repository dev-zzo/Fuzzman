using System.IO;
using System.Threading;
using System.Xml.Serialization;
using Fuzzman.Agent.Config;
using Fuzzman.Core;
using Fuzzman.Core.Mutator;
using System.Text;
using System;
using Fuzzman.Core.Debugger.Simple;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Debugger.DebugInfo;

namespace Fuzzman.Agent
{
    public class Agent : IAgent
    {
        public Agent(string configPath)
        {
            this.config = LoadConfig(configPath);
        }

        public void Start()
        {
            if (this.iterationThread != null)
                return;

            this.iterationThread = new Thread(this.IterationThreadProc);
            this.iterationThread.Start();
        }

        public void Stop()
        {
            this.isStopping = true;
            if (this.iterationThread != null && this.iterationThread.IsAlive)
            {
                this.iterationThread.Join();
                this.iterationThread = null;
            }
        }

        private readonly ILogger logger = LogManager.GetLogger("Agent");
        private Configuration config = null;
        private bool isStopping = false;
        private Thread iterationThread = null;
        private readonly AutoResetEvent testCompletedEvent = new AutoResetEvent(false);
        private FaultReport report = null;

        private static Configuration LoadConfig(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return (Configuration)serializer.Deserialize(stream);
            }
        }

        private static string SaveConfig(Configuration config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, config);
            return writer.ToString();
        }

        private void IterationThreadProc()
        {
            IRandom rng = new StdRandom(12345);
            IMutator bitFlipper = new BitFlipper(rng);
            IMutator valueSetter = new ValueSetter(rng);

            IDebugger debugger = new SimpleDebugger();
            debugger.ProcessCreatedEvent += this.OnProcessCreated;
            debugger.ProcessExitedEvent += this.OnProcessExited;
            debugger.ExceptionEvent += this.OnException;

            try
            {
                int testCaseNumber = 1;
                while (!this.isStopping)
                {
                    // Make test case dir
                    string currentTestCaseDir = Path.Combine(this.config.Agent.TestCasesPath, "CURRENT");
                    if (!Directory.Exists(currentTestCaseDir))
                    {
                        Directory.CreateDirectory(currentTestCaseDir);
                    }

                    // Copy the sample file
                    string sampleFileName = Path.GetFileName(this.config.Agent.SourceFilePath);
                    string samplePath = Path.Combine(currentTestCaseDir, sampleFileName);
                    File.Copy(this.config.Agent.SourceFilePath, samplePath);
                    
                    // Start the target
                    string cmdLine = this.MakeTestCommandLine(samplePath);
                    debugger.StartTarget(cmdLine);

                    // Wait for the program to complete or die.
                    this.testCompletedEvent.WaitOne();

                    // See if there was anything interesting...
                    if (this.report != null)
                    {
                        this.report.Generate(Path.Combine(currentTestCaseDir, "fault-report.txt"));
                        string savedTestCaseDir = MakeTestCaseDir(testCaseNumber);
                        Directory.Move(currentTestCaseDir, savedTestCaseDir);
                    }

                    // Next test case
                    testCaseNumber++;
                }
            }
            catch (Exception ex)
            {
                // Something went wrong... shut down gracefully.
            }
        }

        private string MakeTestCaseDir(int testCaseNumber)
        {
            StringBuilder builder = new StringBuilder(this.config.Agent.TestCaseTemplate, 256);

            builder.Replace("{TCN}", testCaseNumber.ToString("D8"));
            builder.Replace("{DATETIME}", DateTime.Now.ToString("s"));

            return Path.Combine(this.config.Agent.TestCasesPath, builder.ToString());
        }

        private string MakeTestCommandLine(string samplePath)
        {
            StringBuilder builder = new StringBuilder(this.config.Agent.CommandLine, 256);

            builder.Replace("{TARGET}", samplePath);

            return builder.ToString();
        }

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            this.logger.Info(String.Format("Target process started, pid {0}.", info.ProcessId));
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            this.logger.Info("Target process has exited.");
            this.testCompletedEvent.Set();
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info(String.Format("Target process has caused an exception."));
            debugger.TerminateTarget();
        }

        private void BuildExceptionFaultReport(IDebugger debugger, ExceptionEventParams info)
        {
            ExceptionFaultReport thisReport = new ExceptionFaultReport();

            AccessViolationDebugInfo avDebugInfo = info.Info as AccessViolationDebugInfo;
            if (avDebugInfo != null)
            {
                AccessViolationFaultReport report = new AccessViolationFaultReport();
                report.AccessType = avDebugInfo.Type.ToString();
                report.TargetVA = avDebugInfo.TargetVA;
            }

            thisReport.ExceptionCode = info.Info.ExceptionCode;
            thisReport.OffendingVA = info.Info.OffendingVA;

            this.report = thisReport;
        }
    }
}
