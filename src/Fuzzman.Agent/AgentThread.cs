using System;
using System.IO;
using System.Threading;
using Fuzzman.Agent.Config;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Debugger.DebugInfo;
using Fuzzman.Core.Debugger.Simple;
using Fuzzman.Core.Interop;
using Fuzzman.Core.Monitor;

namespace Fuzzman.Agent
{
    internal class AgentThread
    {
        public AgentThread(int id, AgentConfiguration config)
        {
            this.id = id;
            this.config = config;
        }

        public void Start()
        {
            this.thread = new Thread(this.ThreadProc);
            this.thread.Priority = ThreadPriority.Highest;
            this.thread.Start();
        }

        public void Stop()
        {
            this.isStopping = true;
            this.doneWithProcess = true;
            if (this.thread != null && this.thread.IsAlive)
            {
                this.logger.Info("Waiting for agent thread...");
                this.thread.Join();
            }
        }

        private readonly ILogger logger = LogManager.GetLogger("AgentThread");
        private readonly int id;
        private readonly AgentConfiguration config;
        private static int testCaseId = 0;
        private static object testCaseIdLock = new object();
        private Thread thread;
        private bool isStopping = false;
        private FaultReport report = null;
        private bool processStarted = false;
        private bool doneWithProcess = false;

        private int GetNextTestCaseNumber()
        {
            lock (testCaseIdLock)
            {
                return testCaseId++;
            }
        }

        private void ThreadProc()
        {
            int seed = new Random().Next();
            IRandom rng = new StdRandom(seed + this.id * 1777);
            IFuzzer fuzzer = new DumbFuzzer(rng);
            //this.logger.Info("Using RNG seed {0}.", seed);

            try
            {
                int testCaseNumber = GetNextTestCaseNumber();
                int rerunCount = 0;
                TestCase currentTest = null;

                while (!this.isStopping)
                {
                    if (rerunCount <= 0)
                    {
                        this.logger.Info("[{0}] *** Test case {1} starting.", this.id, testCaseNumber);

                        uint sourceIndex = rng.GetNext(0, (uint)this.config.Sources.Length);
                        string currentSource = this.config.Sources[sourceIndex];
                        this.logger.Info("[{0}] Using source: {1}", this.id, currentSource);

                        currentTest = new TestCase(testCaseNumber);
                        currentTest.WorkingDirectory = Path.Combine(this.config.TestCasesPath, Path.GetRandomFileName());
                        currentTest.SourceFilePath = currentSource;
                        currentTest.SaveDirectory = this.config.TestCasesPath;
                        currentTest.TestCaseTemplate = this.config.TestCaseTemplate;
                        currentTest.CommandLineTemplate = this.config.CommandLine;
                        currentTest.Setup(fuzzer);
                    }
                    else
                    {
                        this.logger.Info("[{0}] *** Test case {1} running again.", this.id, testCaseNumber);
                    }

                    IDebugger debugger = new SimpleDebugger();
                    debugger.ProcessCreatedEvent += this.OnProcessCreated;
                    debugger.ProcessExitedEvent += this.OnProcessExited;
                    debugger.ExceptionEvent += this.OnException;

                    // Start the target
                    this.processStarted = false;
                    this.doneWithProcess = false;
                    debugger.CreateTarget(currentTest.GetCommandLine());

                    // Sync to process creation.
                    while (!this.processStarted)
                    {
                        debugger.WaitAndDispatchEvent();
                    }

                    ProcessIdleMonitor mon = new ProcessIdleMonitor(debugger.Process.Pid);
                    mon.IdleEvent += new ProcessIdleEventHandler(this.OnProcessIdle);
                    mon.MaxIdleCount = 25;
                    mon.CheckContextSwitches = true;
                    mon.Start();

                    // Wait for the program to complete or die.
                    this.logger.Info("[{0}] Waiting for the target to die.", this.id);
                    DateTime startTime = DateTime.Now;
                    TimeSpan timeoutSpan = TimeSpan.FromSeconds(this.config.Timeout);
                    while (!this.doneWithProcess)
                    {
                        debugger.WaitAndDispatchEvent();
                        if (DateTime.Now - startTime > timeoutSpan)
                        {
                            break;
                        }
                    }
                    mon.Stop();
                    debugger.TerminateTarget();
                    debugger.Dispose();

                    if (this.report != null)
                    {
                        this.logger.Info("[{0}] Caught a fault.", this.id);
                        currentTest.Reports.Add(this.report);
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
                                this.logger.Info("[{0}] Reproducible test case, saving.", this.id);
                                currentTest.SaveResults();
                            }
                        }
                    }
                    else
                    {
                        this.logger.Info("[{0}] Nothing interesting happened.", this.id);
                        if (rerunCount > 0)
                        {
                            this.logger.Info("[{0}] Discarding the test case -- failed to reproduce on rerun {1}.", this.id, rerunCount);
                        }
                        rerunCount = 0;
                    }

                    if (rerunCount <= 0)
                    {
                        this.logger.Info("[{0}] *** Test case ended.", this.id);
                        currentTest.Cleanup();

                        // Next test case
                        testCaseNumber = GetNextTestCaseNumber();
                    }
                }
            }
            catch (Exception ex)
            {
                // Something went wrong... shut down gracefully.
                this.logger.Fatal("[{0}] The agent thread has crashed with exception:\r\n{1}", this.id, ex.ToString());
            }
        }

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            //this.logger.Info("Target process started, pid {0}.", info.ProcessId);
            this.processStarted = true;
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            //this.logger.Info("Target process has exited.");
            this.doneWithProcess = true;
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info("Target has raised an exception {0:X8} at {1:X8} in thread {2}.",
                (uint)info.Info.ExceptionCode,
                (uint)info.Info.OffendingVA,
                info.ThreadId);

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
            this.doneWithProcess = true;
        }

        private void OnProcessIdle()
        {
            this.logger.Info("[{0}] Target process detected to be idle, killing.", this.id);
            this.doneWithProcess = true;
        }
    }
}
