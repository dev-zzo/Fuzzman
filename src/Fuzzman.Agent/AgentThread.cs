using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Fuzzman.Agent.Actions;
using Fuzzman.Agent.Config;
using Fuzzman.Agent.Fuzzers;
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
            this.workerId = id;
            this.config = config;
        }

        public void Start()
        {
            this.isStopping = false;
            this.thread = new Thread(this.ThreadProc);
            this.thread.Priority = ThreadPriority.Highest;
            this.thread.Start();
        }

        public void Stop()
        {
            this.isStopping = true;
            if (this.thread != null && this.thread.IsAlive)
            {
                this.logger.Info("Waiting for agent thread...");
                this.thread.Join();
            }
        }

        private enum State
        {
            InitialState,

            SetupTestCase,
            StartTarget,
            MonitorTarget,
            TerminateTarget,
            AnalyseState,
            CleanupTestCase,

            HandleFailure,

            Stopped,
        }

        private readonly ILogger logger = LogManager.GetLogger("AgentThread");
        private readonly int workerId;
        private readonly AgentConfiguration config;
        private Thread thread;
        private bool isStopping;
        private State state;
        private Exception failureReason;
        private IDebugger debugger;
        private List<IMonitor> monitors;
        private uint targetPid;
        private TestCase testCase;
        private bool processStarted;
        private bool doneWithTarget;

        private void ThreadProc()
        {
            int seed = new Random().Next();
            IRandom rng = new StdRandom(seed + this.workerId * 1777);
            IFuzzer fuzzer = new DumbFuzzer(rng);

            this.state = State.InitialState;
            this.failureReason = null;
            this.CreateMonitors();

            while (this.state != State.Stopped)
            {
                switch (this.state)
                {
                    case State.InitialState:
                        this.state = State.SetupTestCase;
                        break;

                    case State.SetupTestCase:
                        this.state = this.CreateNewTestCase(rng, fuzzer);
                        break;

                    case State.StartTarget:
                        this.state = this.StartTarget();
                        break;

                    case State.MonitorTarget:
                        this.state = this.MonitorTarget();
                        break;

                    case State.TerminateTarget:
                        this.state = this.TerminateTarget();
                        break;

                    case State.AnalyseState:
                        this.state = this.AnalyseState();
                        break;

                    case State.CleanupTestCase:
                        this.state = this.CleanupTestCase();
                        break;

                    case State.HandleFailure:
                        this.state = this.HandleFailure();
                        break;

                    default:
                        break;
                }
            }

            this.TerminateMonitors();
        }

        #region States handling

        private State CreateNewTestCase(IRandom rng, IFuzzer fuzzer)
        {
            testCase = new TestCase();

            this.logger.Info("[{0}] *** Test case #{1} setting up.", this.workerId, testCase.TestCaseNumber);

            uint sourceIndex = rng.GetNext(0, (uint)this.config.Sources.Length);
            string currentSource = this.config.Sources[sourceIndex];
            this.logger.Debug("[{0}] Using source: {1}", this.workerId, currentSource);

            testCase.WorkingDirectory = Path.Combine(this.config.TestCasesPath, Path.GetRandomFileName());
            testCase.SourceFilePath = currentSource;
            testCase.SaveDirectory = this.config.TestCasesPath;
            testCase.TestCaseTemplate = this.config.TestCaseTemplate;
            testCase.CommandLineTemplate = this.config.CommandLine;
            testCase.Setup(fuzzer);

            return State.StartTarget;
        }

        private State StartTarget()
        {
            this.logger.Info("[{0}] Starting the target.", this.workerId);

            this.debugger = new SimpleDebugger();
            this.debugger.ProcessCreatedEvent += this.OnProcessCreated;
            this.debugger.ProcessExitedEvent += this.OnProcessExited;
            this.debugger.ThreadCreatedEvent += this.OnThreadCreated;
            this.debugger.ThreadExitedEvent += this.OnThreadExited;
            this.debugger.ExceptionEvent += this.OnException;

            this.processStarted = false;
            this.doneWithTarget = false;
            this.targetPid = 0;

            this.debugger.CreateTarget(testCase.CommandLine);

            this.testCase.StartTime = DateTime.Now;
            this.testCase.RunCount++;

            // Sync to process creation.
            while (!this.processStarted)
            {
                this.debugger.WaitAndDispatchEvent();
            }

            this.AttachMonitors(this.targetPid);

            this.logger.Info("[{0}] Target has been started.", this.workerId);
            return State.MonitorTarget;
        }

        private State MonitorTarget()
        {
            this.debugger.WaitAndDispatchEvent();

            // Could probably push this out into a separate monitor...
            // But it's too damn simple.
            if (DateTime.Now - this.testCase.StartTime > TimeSpan.FromSeconds(this.config.Timeout))
            {
                this.logger.Info("[{0}] Timeout hit, terminating the target.", this.workerId);
                return State.TerminateTarget;
            }

            if (this.isStopping)
            {
                this.logger.Info("[{0}] Stopping flag set, wrapping things up.", this.workerId);
                return State.TerminateTarget;
            }

            return this.state;
        }

        private State TerminateTarget()
        {
            this.DetachMonitors();

            if (this.debugger != null)
            {
                this.logger.Info("[{0}] Terminating the target.", this.workerId);
                this.debugger.TerminateTarget();

                while (!this.doneWithTarget)
                {
                    this.debugger.WaitAndDispatchEvent();
                }

                this.debugger.Dispose();
                this.debugger = null;
            }

            if (this.config.PostRunActions != null)
            {
                this.logger.Info("[{0}] Running post-run actions...", this.workerId);
                ActionBase.Execute(this.config.PostRunActions);
            }

            if (this.isStopping || this.failureReason != null)
            {
                return State.CleanupTestCase;
            }

            return State.AnalyseState;
        }

        private State AnalyseState()
        {
            if (this.testCase.RunCount == 1)
            {
                // This is the first run of this test case.
                // If there were no results reported, junk it.
                // Otherwise, restart it.

                if (this.testCase.Reports.Count == 0)
                {
                    this.logger.Info("[{0}] Test case produced no results, junking.", this.workerId);
                    return State.CleanupTestCase;
                }
                else
                {
                    return State.StartTarget;
                }
            }

            // This is 2+ run of this test case.
            // Go on running until we hit the limit.
            if (this.testCase.RunCount < this.config.RunCount)
            {
                return State.StartTarget;
            }

            TestCaseAnalyser analyser = new TestCaseAnalyser(this.testCase);
            analyser.Analyse();
            if (analyser.IsInteresting)
            {
                this.testCase.SaveResults(analyser.ReportSummary, analyser.ReportText);
            }

            return State.CleanupTestCase;
        }

        private State CleanupTestCase()
        {
            if (this.testCase != null)
            {
                this.testCase.Cleanup();
                this.testCase = null;
            }

            this.failureReason = null;

            if (this.isStopping)
            {
                return State.Stopped;
            }

            // TODO: handle single test run.
            return State.SetupTestCase;
        }

        private State HandleFailure()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            this.logger.Error("[{0}] The agent thread has thrown an exception:\r\n{1}", this.workerId, this.failureReason.ToString());

            return State.TerminateTarget;
        }

        #endregion

        #region Monitors handling

        private void CreateMonitors()
        {
            this.monitors = new List<IMonitor>();

            // TODO: Think of a better way of doing this...
            if (this.config.ProcessIdleMonitor != null)
            {
                ProcessIdleMonitor pim = new ProcessIdleMonitor(this.config.ProcessIdleMonitor);
                pim.KillTargetEvent += new KillTargetEventHandler(this.OnKillProcess);
                this.monitors.Add(pim);
            }

            foreach (IMonitor monitor in this.monitors)
            {
                monitor.Start();
            }
        }

        private void AttachMonitors(uint pid)
        {
            foreach (IMonitor monitor in this.monitors)
            {
                monitor.Attach(pid);
            }
        }

        private void DetachMonitors()
        {
            foreach (IMonitor monitor in this.monitors)
            {
                monitor.Detach();
            }
        }

        private void TerminateMonitors()
        {
            foreach (IMonitor monitor in this.monitors)
            {
                monitor.Stop();
            }
        }

        #endregion

        #region Event handlers

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            string processPath = debugger.Processes[info.ProcessId].ImagePath;
            this.logger.Info("[{0}] Process {1} created ({2}).", this.workerId, info.ProcessId, processPath);
            if (String.IsNullOrEmpty(this.config.ProcessName) || Path.GetFileName(processPath).ToLower() == this.config.ProcessName.ToLower())
            {
                this.targetPid = info.ProcessId;
                this.processStarted = true;
            }
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            this.logger.Info("[{0}] Process {1} terminated.", this.workerId, info.ProcessId);
            if (info.ProcessId == this.targetPid)
            {
                this.state = State.TerminateTarget;
            }
            this.logger.Debug("[{0}] Active processes count: {1}.", this.workerId, this.debugger.Processes.Count);
            if (!this.doneWithTarget)
            {
                this.doneWithTarget = this.debugger.Processes.Count == 0;
            }
        }

        private void OnThreadCreated(IDebugger debugger, ThreadCreatedEventParams info)
        {
            this.logger.Info("[{0}] Thread {1}/{2} created.", this.workerId, info.ThreadId, info.ProcessId);
        }

        private void OnThreadExited(IDebugger debugger, ThreadExitedEventParams info)
        {
            this.logger.Info("[{0}] Thread {1}/{2} terminated.", this.workerId, info.ThreadId, info.ProcessId);
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info("[{0}] Caught an exception {1:X8} at {2:X8} in thread {3}.",
                this.workerId,
                (uint)info.Info.ExceptionCode,
                (uint)info.Info.OffendingVA,
                info.ThreadId);

            if (this.state != State.MonitorTarget)
            {
                this.logger.Error("[{0}] Bogus exception -- the target is not being monitored.", this.workerId);
                return;
            }

            if (this.config.PassExceptions != null)
            {
                foreach (string passedExceptionName in this.config.PassExceptions)
                {
                    try
                    {
                        EXCEPTION_CODE passedException = (EXCEPTION_CODE)Enum.Parse(typeof(EXCEPTION_CODE), passedExceptionName);
                        if (info.IsFirstChance && info.Info.ExceptionCode == passedException)
                        {
                            this.logger.Info("[{0}] Passing the exception to the application (first-chance).", this.workerId);
                            return;
                        }
                    }
                    catch (ArgumentException)
                    {
                        this.logger.Error("Unrecognized exception name: {0}", passedExceptionName);
                    }
                }
            }

            FaultReport thisReport = BuildFaultReport(debugger, info);
            this.testCase.Reports.Add(thisReport);
            this.state = State.TerminateTarget;
        }

        private void OnKillProcess()
        {
            this.logger.Info("[{0}] Monitors report the process may be killed now.", this.workerId);
            this.state = State.TerminateTarget;
        }

        #endregion

        private FaultReport BuildFaultReport(IDebugger debugger, ExceptionEventParams info)
        {
            ThreadInfo threadInfo = debugger.Threads[info.ThreadId];
            ProcessInfo processInfo = debugger.Processes[info.ProcessId];

            AccessViolationDebugInfo avDebugInfo = info.Info as AccessViolationDebugInfo;
            if (avDebugInfo != null)
            {
                AccessViolationFaultReport report = new AccessViolationFaultReport();
                this.FillFaultReport(debugger, threadInfo, processInfo, avDebugInfo, report);
                return report;
            }

            {
                ExceptionFaultReport report = new ExceptionFaultReport();
                this.FillFaultReport(debugger, threadInfo, processInfo, info.Info, report);
                return report;
            }
        }

        private void FillFaultReport(IDebugger debugger, ThreadInfo threadInfo, ProcessInfo processInfo, ExceptionDebugInfo debugInfo, ExceptionFaultReport report)
        {
            report.ExceptionCode = debugInfo.ExceptionCode;
            report.OffendingVA = debugInfo.OffendingVA;

            report.Location = DebuggerHelper.LocateModuleOffset(processInfo.Handle, processInfo.PebLinearAddress, debugInfo.OffendingVA);

            report.Context = new CONTEXT();
            report.Context.ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL;
            if (!Kernel32.GetThreadContext(threadInfo.Handle, ref report.Context))
            {
                this.logger.Error("[{0}] Failed to obtain the thread's context.", this.workerId);
            }

            // TODO: Bitness warning.
            int stackTraceCount = 32;
            int stackTraceSize = stackTraceCount * 4;
            byte[] rawStack = new byte[stackTraceSize];
            uint bytesRead;
            if (Kernel32.ReadProcessMemory(processInfo.Handle, (IntPtr)report.Context.Esp, rawStack, (uint)stackTraceSize, out bytesRead))
            {
                // Make sure we process only as many bytes as were actually read.
                stackTraceSize = (int)bytesRead;
                stackTraceCount = stackTraceSize / 4;

                report.StackDump = new uint[stackTraceCount];
                for (int i = 0; i < stackTraceCount; ++i)
                {
                    report.StackDump[i] = BitConverter.ToUInt32(rawStack, i * 4);
                }
            }
        }

        private void FillFaultReport(IDebugger debugger, ThreadInfo threadInfo, ProcessInfo processInfo, AccessViolationDebugInfo debugInfo, AccessViolationFaultReport report)
        {
            this.FillFaultReport(debugger, threadInfo, processInfo, (ExceptionDebugInfo)debugInfo, (ExceptionFaultReport)report);
            report.AccessType = debugInfo.Type.ToString();
            report.TargetVA = debugInfo.TargetVA;
        }

    }
}
