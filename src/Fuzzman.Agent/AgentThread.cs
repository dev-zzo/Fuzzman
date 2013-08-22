using System;
using System.IO;
using System.Threading;
using Fuzzman.Agent.Actions;
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
        private IDebugger debugger;
        private uint targetPid;
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

            int testCaseNumber = GetNextTestCaseNumber();
            int rerunCount = 0;
            TestCase currentTest = null;
            while (!this.isStopping)
            {
                try
                {
                    if (rerunCount == 0)
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

                    using (this.debugger = new SimpleDebugger())
                    {
                        this.debugger.ProcessCreatedEvent += this.OnProcessCreated;
                        this.debugger.ProcessExitedEvent += this.OnProcessExited;
                        this.debugger.ExceptionEvent += this.OnException;

                        // Start the target
                        this.processStarted = false;
                        this.doneWithProcess = false;
                        this.targetPid = 0;
                        this.debugger.CreateTarget(currentTest.GetCommandLine());

                        ProcessIdleMonitorConfig monConfig = this.config.ProcessIdleMonitor != null ? this.config.ProcessIdleMonitor : new ProcessIdleMonitorConfig();
                        ProcessIdleMonitor mon = new ProcessIdleMonitor(monConfig);
                        mon.IdleEvent += new ProcessIdleEventHandler(this.OnProcessIdle);
                        mon.Start();

                        // Sync to process creation.
                        while (!this.processStarted)
                        {
                            this.debugger.WaitAndDispatchEvent();
                        }

                        // Wait for the program to complete or die.
                        this.logger.Info("[{0}] Waiting for the target exit/kill.", this.id);
                        mon.ProcessId = this.targetPid;
                        DateTime startTime = DateTime.Now;
                        TimeSpan timeoutSpan = TimeSpan.FromSeconds(this.config.Timeout);
                        bool isTerminating = false;
                        while (!this.doneWithProcess)
                        {
                            this.debugger.WaitAndDispatchEvent();
                            if (DateTime.Now - startTime > timeoutSpan && !isTerminating)
                            {
                                isTerminating = true;
                                this.debugger.TerminateTarget();
                            }
                        }
                        mon.Stop();
                    }
                    this.debugger = null;

                    if (this.report != null)
                    {
                        this.logger.Info("[{0}] Caught a fault.", this.id);
                        currentTest.Reports.Add(this.report);
                        this.report = null;
                    }
                    else
                    {
                        this.logger.Info("[{0}] Nothing interesting happened.", this.id);
                    }

                    if (this.config.PostRunActions != null)
                    {
                        this.logger.Info("[{0}] Running post-run actions...", this.id);
                        ActionBase.Execute(this.config.PostRunActions);
                    }

                    if (rerunCount == 0)
                    {
                        if (currentTest.Reports.Count > 0)
                        {
                            rerunCount = this.config.RerunCount;
                        }
                    }
                    else
                    {
                        rerunCount--;
                    }

                    if (rerunCount == 0)
                    {
                        if (currentTest.Reports.Count > 0)
                        {
                            this.logger.Info("[{0}] Something interesting, saving.", this.id);
                            currentTest.SaveResults();
                        }

                        this.logger.Info("[{0}] *** Test case ended.", this.id);
                        currentTest.Cleanup();
                        currentTest = null;

                        // Next test case
                        testCaseNumber = GetNextTestCaseNumber();
                    }
                }
                catch (Exception ex)
                {
                    // Something went wrong... log the exception and continue running.
                    this.logger.Error("[{0}] The agent thread has thrown an exception:\r\n{1}", this.id, ex.ToString());
                    if (this.debugger != null)
                    {
                        this.debugger.TerminateTarget();
                    }
                    if (currentTest != null)
                    {
                        currentTest.Cleanup();
                    }
                }
            }
        }

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            string processPath = debugger.Processes[info.ProcessId].ImagePath;
            this.logger.Info("[{0}] Process started, pid {1} ({2}).", this.id, info.ProcessId, processPath);
            if (String.IsNullOrEmpty(this.config.ProcessName) || Path.GetFileName(processPath).ToLower() == this.config.ProcessName.ToLower())
            {
                this.targetPid = info.ProcessId;
                this.processStarted = true;
            }
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            this.logger.Info("[{0}] Process has terminated, pid {1}.", this.id, info.ProcessId);
            if (info.ProcessId == this.targetPid)
            {
                this.debugger.TerminateTarget();
            }
            this.doneWithProcess = this.debugger.Processes.Count == 0;
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info("[{0}] Caught an exception {1:X8} at {2:X8} in thread {3}.",
                this.id,
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
            debugger.TerminateTarget();
        }

        private void OnProcessIdle()
        {
            this.logger.Info("[{0}] Target process detected to be idle, killing.", this.id);
            this.debugger.TerminateTarget();
        }
    }
}
