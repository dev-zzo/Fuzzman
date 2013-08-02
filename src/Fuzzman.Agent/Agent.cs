using System;
using System.IO;
using System.Text;
using System.Threading;
using Fuzzman.Agent.Config;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Debugger.Simple;
using Fuzzman.Core.Monitor;
using Fuzzman.Core.Interop;

namespace Fuzzman.Agent
{
    public class Agent : IAgent
    {
        public Agent(Options options, AgentConfiguration config)
        {
            this.options = options;
            this.config = config;
            this.ctrlCHandler = new ConsoleCancelEventHandler(this.OnControlC);
        }

        public void Start()
        {
            if (this.iterationThread != null)
                return;

            Console.CancelKeyPress += this.ctrlCHandler;

            // http://msdn.microsoft.com/en-us/library/windows/hardware/ff545528%28v=vs.85%29.aspx
            Environment.SetEnvironmentVariable("_NO_DEBUG_HEAP", "1");

            this.logger.Info("Starting agent thread...");
            this.iterationThread = new Thread(this.IterationThreadProc);
            this.iterationThread.Start();
        }

        public void Stop()
        {
            this.logger.Info("Stopping agent thread...");
            this.isStopping = true;
            this.processExitedEvent.Set();
            if (this.iterationThread != null && this.iterationThread.IsAlive)
            {
                if (this.debugger != null)
                {
                    this.logger.Info("Killing the target...");
                    this.debugger.TerminateTarget();
                }
                this.logger.Info("Waiting for agent thread...");
                this.iterationThread.Join();
                this.iterationThread = null;
            }
        }

        private readonly ILogger logger = LogManager.GetLogger("Agent");
        private readonly Options options = null;
        private readonly AgentConfiguration config = null;
        private readonly ConsoleCancelEventHandler ctrlCHandler;
        private bool isStopping = false;
        private Thread iterationThread = null;
        private IDebugger debugger = null;
        private readonly AutoResetEvent processCreatedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent processExitedEvent = new AutoResetEvent(false);
        private TestCase currentTest = null;
        private FaultReport report = null;

        private void IterationThreadProc()
        {
            int seed = options.RandomSeed;
            if (seed == 0)
                seed = new Random().Next();
            IRandom rng = new StdRandom(seed);
            IFuzzer fuzzer = new DumbFuzzer(rng);
            this.logger.Info(String.Format("Using RNG seed {0}.", seed));

            this.debugger = new SimpleDebugger();
            this.debugger.ProcessCreatedEvent += this.OnProcessCreated;
            this.debugger.ProcessExitedEvent += this.OnProcessExited;
            this.debugger.ExceptionEvent += this.OnException;

            this.logger.Info("*** Test sequence starting.\r\n");
            try
            {
                int testCaseNumber = 1;
                int rerunCount = 0;

                while (!this.isStopping)
                {
                    bool skipThis = testCaseNumber < options.SkipIterations;

                    if (rerunCount <= 0)
                    {
                        this.logger.Info(String.Format("*** Test case {0} starting.", testCaseNumber));

                        uint sourceIndex = rng.GetNext(0, (uint)this.config.Sources.Length);
                        string currentSource = this.config.Sources[sourceIndex];
                        this.logger.Info(String.Format("Using source: {0}", currentSource));

                        this.currentTest = new TestCase(testCaseNumber);
                        this.currentTest.WorkingDirectory = Path.Combine(this.config.TestCasesPath, "CURRENT");
                        this.currentTest.SourceFilePath = currentSource;
                        this.currentTest.SaveDirectory = this.config.TestCasesPath;
                        this.currentTest.TestCaseTemplate = this.config.TestCaseTemplate;
                        this.currentTest.CommandLineTemplate = this.config.CommandLine;
                        this.currentTest.Setup(fuzzer);
                    }
                    else
                    {
                        this.logger.Info(String.Format("*** Test case {0} running again.", testCaseNumber));
                    }

                    // Start the target
                    if (!skipThis)
                    {
                        this.processCreatedEvent.Reset();
                        this.processExitedEvent.Reset();

                        this.currentTest.Start(this.debugger);

                        // Sync to process creation.
                        this.processCreatedEvent.WaitOne();

                        ProcessIdleMonitor mon = new ProcessIdleMonitor(this.debugger.DebuggeePid);
                        mon.IdleEvent += new ProcessIdleEventHandler(this.OnProcessIdle);
                        mon.MaxIdleCount = 25;
                        mon.CheckContextSwitches = true;
                        mon.Start();

                        // Wait for the program to complete or die.
                        this.logger.Info("Waiting for the target to die.");
                        this.processExitedEvent.WaitOne(this.config.Timeout * 1000);
                        mon.Stop();

                        // See if we timed out waiting for the fun.
                        if (this.debugger.IsRunning)
                        {
                            this.logger.Info("Timed out, killing the target.");
                            this.debugger.TerminateTarget();
                            Thread.Sleep(1000);
                        }

                        this.debugger.Stop();
                    }
                    else
                    {
                        this.logger.Info("Test case skipped.");
                    }

                    if (this.report != null)
                    {
                        this.logger.Info("Caught a fault.");
                        this.currentTest.Reports.Add(this.report);
                        this.report = null;
                        if (rerunCount == 0)
                        {
                            rerunCount = this.config.RerunCount;
                        }
                        else
                        {
                            rerunCount--;
                            if (rerunCount == 0)
                            {
                                this.logger.Info("Reproducible test case, saving.");
                                this.currentTest.SaveResults();
                            }
                        }
                    }
                    else
                    {
                        this.logger.Info("Nothing interesting happened.");
                        if (rerunCount > 0)
                        {
                            this.logger.Info(String.Format("Discarding the test case -- failed to reproduce on rerun {0}.", rerunCount));
                        }
                        rerunCount = 0;
                    }

                    if (rerunCount <= 0)
                    {
                        this.logger.Info("*** Test case ended.\r\n");
                        this.currentTest.Cleanup();
 
                        // Next test case
                        testCaseNumber++;
                   }
                }
            }
            catch (Exception ex)
            {
                // Something went wrong... shut down gracefully.
                this.logger.Fatal(String.Format("The agent thread has crashed with exception:\r\n{0}", ex.ToString()));
            }
            finally
            {
                this.debugger.Dispose();
            }
            this.logger.Info("*** Test sequence ended.\r\n");
        }

        private void OnControlC(object sender, ConsoleCancelEventArgs args)
        {
            this.logger.Info("Caught ctrl-c, stopping.");
            this.Stop();
        }

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            this.logger.Info(String.Format("Target process started, pid {0}.", info.ProcessId));
            this.processCreatedEvent.Set();
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            this.logger.Info("Target process has exited.");
            this.processExitedEvent.Set();
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info(String.Format("Target has raised an exception {0:X8} at {1:X8} in thread {2}.",
                (uint)info.Info.ExceptionCode,
                (uint)info.Info.OffendingVA,
                info.ThreadId));

            ExceptionFaultReport thisReport = new ExceptionFaultReport();

            AccessViolationDebugInfo avDebugInfo = info.Info as AccessViolationDebugInfo;
            if (avDebugInfo != null)
            {
                AccessViolationFaultReport report = new AccessViolationFaultReport();
                report.AccessType = avDebugInfo.Type.ToString();
                report.TargetVA = avDebugInfo.TargetVA;
                thisReport = report;
            }

            thisReport.ExceptionCode = info.Info.ExceptionCode;
            thisReport.OffendingVA = info.Info.OffendingVA;

            ThreadInfo threadInfo = debugger.Threads[info.ThreadId];
            CONTEXT context = new CONTEXT();
            Kernel32.GetThreadContext(threadInfo.Handle, ref context);
            thisReport.RegisterDump = context.ToString();

            this.report = thisReport;

            this.debugger.TerminateTarget();
        }

        private void OnProcessIdle()
        {
            this.logger.Info("Target process detected to be idle, killing.");
            this.debugger.TerminateTarget();
        }
    }
}
