﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Fuzzman.Agent.Config;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Debugger.Simple;
using Fuzzman.Core.Mutator;

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

            this.logger.Info("Starting agent thread...");
            this.iterationThread = new Thread(this.IterationThreadProc);
            this.iterationThread.Start();
        }

        public void Stop()
        {
            this.isStopping = true;
            if (this.iterationThread != null && this.iterationThread.IsAlive)
            {
                this.logger.Info("Stopping agent thread...");
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

        private void IterationThreadProc()
        {
            IRandom rng = new StdRandom(12345);
            IMutator bitFlipper = new BitFlipper(rng);
            IMutator valueSetter = new ValueSetter(rng);

            IDebugger debugger = new SimpleDebugger();
            debugger.ProcessCreatedEvent += this.OnProcessCreated;
            debugger.ProcessExitedEvent += this.OnProcessExited;
            debugger.ExceptionEvent += this.OnException;

            this.logger.Info("*** Test sequence starting.\r\n");
            try
            {
                int testCaseNumber = 1;
                while (!this.isStopping)
                {
                    this.logger.Info(String.Format("*** Test case {0} starting.", testCaseNumber));

                    // Make test case dir
                    string currentTestCaseDir = Path.Combine(this.config.Agent.TestCasesPath, "CURRENT");
                    if (Directory.Exists(currentTestCaseDir))
                    {
                        Directory.Delete(currentTestCaseDir, true);
                    }
                    Directory.CreateDirectory(currentTestCaseDir);

                    // Copy the sample file
                    this.logger.Info("Preparing the sample file...");
                    string sampleFileName = Path.GetFileName(this.config.Agent.SourceFilePath);
                    string samplePath = Path.Combine(currentTestCaseDir, sampleFileName);
                    File.Copy(this.config.Agent.SourceFilePath, samplePath);

                    // Fuzz the sample
                    this.logger.Info("Fuzzing the sample file...");
                    
                    // Start the target
                    string cmdLine = this.MakeTestCommandLine(samplePath);
                    this.logger.Info(String.Format("Starting the target with: {0}", cmdLine));
                    debugger.StartTarget(cmdLine);

                    // Wait for the program to complete or die.
                    this.logger.Info("Waiting for the target to die.");
                    this.testCompletedEvent.WaitOne(this.config.Agent.Timeout * 1000);

                    if (debugger.IsRunning)
                    {
                        this.logger.Info("Timed out, killing the target.");
                        debugger.TerminateTarget();
                    }
                    else
                    {
                        this.logger.Info("Target either crashed or exited, good.");
                    }
                    debugger.Stop();

                    // See if there was anything interesting...
                    if (this.report != null)
                    {
                        this.logger.Info("Caught a fault, saving the test data.");
                        this.report.Generate(Path.Combine(currentTestCaseDir, "fault-report.txt"));
                        string savedTestCaseDir = this.MakeTestCaseDir(testCaseNumber);
                        Directory.Move(currentTestCaseDir, savedTestCaseDir);
                    }
                    else
                    {
                        this.logger.Info("Nothing interesting happened.");
                    }

                    this.logger.Info("*** Test case ended.\r\n");

                    // Next test case
                    testCaseNumber++;
                }
            }
            catch (Exception ex)
            {
                // Something went wrong... shut down gracefully.
                this.logger.Fatal(String.Format("The agent thread has crashed with exception:\r\n{0}", ex.ToString()));
            }
            finally
            {
                debugger.Dispose();
            }
            this.logger.Info("*** Test sequence ended.\r\n");
        }

        private string MakeTestCaseDir(int testCaseNumber)
        {
            StringBuilder builder = new StringBuilder(this.config.Agent.TestCaseTemplate, 256);

            builder.Replace("{TCN}", testCaseNumber.ToString("D8"));
            builder.Replace("{DATETIME}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));

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
            this.BuildExceptionFaultReport(debugger, info);
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
            thisReport.RegisterDump = debugger.GetThreadContext(info.ThreadId).ToString();

            this.report = thisReport;
        }
    }
}
