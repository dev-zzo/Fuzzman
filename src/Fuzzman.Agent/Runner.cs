using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// Runs the target application, watching for events.
    /// </summary>
    public sealed class Runner
    {
        public Runner(AgentConfiguration config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;

            this.state = State.InitialState;
        }

        public TestRun RunTestCase(string commandLine)
        {
            this.commandLine = commandLine;
            this.thisRun = new TestRun();

            this.thread = new Thread(this.ThreadProc);
            this.thread.Priority = ThreadPriority.Highest;
            this.thread.Start();
            this.thread.Join();

            return this.thisRun;
        }

        public void AbortTestCase()
        {
            this.aborting = true;
        }

        #region Implementation details

        private enum State
        {
            InitialState,

            StartTarget,
            MonitorTarget,
            StopTarget,
            TerminateTarget,

            HandleFailure,

            Cleanup,
            Stopped,
        }

        private readonly AgentConfiguration config;
        private readonly ILogger logger;
        private State state;
        private Thread thread;
        private string commandLine;
        private readonly List<IProcessMonitor> monitors = new List<IProcessMonitor>();
        private IDebugger debugger;
        private uint targetPid;
        private bool doneWithTarget; // True when all the processes have exited.
        private Exception failureReason;
        private bool aborting;
        private TestRun thisRun;

        private void ThreadProc()
        {
            while (this.state != State.Stopped)
            {
                try
                {
                    switch (this.state)
                    {
                        case State.InitialState:
                            this.state = this.Initialize();
                            break;

                        case State.StartTarget:
                            this.state = this.StartTarget();
                            break;

                        case State.MonitorTarget:
                            this.state = this.MonitorTarget();
                            break;

                        case State.StopTarget:
                            this.state = this.StopTarget();
                            break;

                        case State.TerminateTarget:
                            this.state = this.TerminateTarget();
                            break;

                        case State.HandleFailure:
                            this.state = this.HandleFailure();
                            break;

                        case State.Cleanup:
                            this.state = this.Cleanup();
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (this.failureReason == null)
                    {
                        this.failureReason = ex;
                        this.state = State.HandleFailure;
                    }
                    else
                    {
                        this.logger.Error("The runner thread has thrown an exception when handling the previous failure:\r\n{0}", ex.ToString());
                        this.state = State.Stopped;
                    }
                }
            }
        }

        #region States implementation

        private State Initialize()
        {
            return State.StartTarget;
        }

        private State StartTarget()
        {
            this.logger.Info("Starting the target.");

            this.CreateMonitors();

            this.debugger = new SimpleDebugger();
            this.debugger.ProcessCreatedEvent += this.OnProcessCreated;
            this.debugger.ProcessExitedEvent += this.OnProcessExited;
            this.debugger.ThreadCreatedEvent += this.OnThreadCreated;
            this.debugger.ThreadExitedEvent += this.OnThreadExited;
            this.debugger.ExceptionEvent += this.OnException;

            this.debugger.CreateTarget(commandLine);

            // Sync to process creation.
            while (this.targetPid == 0)
            {
                this.debugger.WaitAndDispatchEvent();
            }

            this.AttachMonitors(this.targetPid);

            this.logger.Info("Target has been started.");
            return State.MonitorTarget;
        }

        private State MonitorTarget()
        {
            if (this.aborting)
            {
                this.logger.Info("Aborting flag is set, wrapping things up.");
                return State.TerminateTarget;
            }

            this.debugger.WaitAndDispatchEvent();

            if (this.doneWithTarget)
            {
                // Can happen if the process closes.
                // Clean up, as in this case, we didn't get a thing.
                return State.Cleanup;
            }

            // Stay within the current state, whatever it is.
            return this.state;
        }

        private State StopTarget()
        {
            // Hacky, but seems to be the best place for this.
            // We don't even try to stop console apps gracefully.
            if (this.config.IsConsoleApp)
            {
                return State.TerminateTarget;
            }

            this.DetachMonitors();

            if (this.debugger != null)
            {
                if (this.targetPid != 0)
                {
                    this.logger.Info("Stopping the target.");

                    // Trying to do this gently....
                    Process targetProc = Process.GetProcessById((int)this.targetPid);
                    targetProc.CloseMainWindow();

                    DateTime waitStart = DateTime.Now;
                    while (!this.doneWithTarget)
                    {
                        this.debugger.WaitAndDispatchEvent();

                        // Hardcoded for now...
                        if (DateTime.Now - waitStart >= new TimeSpan(0, 0, 10))
                        {
                            return State.TerminateTarget;
                        }
                    }
                }
            }

            return State.Cleanup;
        }

        private State TerminateTarget()
        {
            this.DetachMonitors();

            if (this.debugger != null)
            {
                if (this.targetPid != 0)
                {
                    this.logger.Info("Terminating the target.");

                    this.debugger.TerminateTarget();

                    while (!this.doneWithTarget)
                    {
                        this.debugger.WaitAndDispatchEvent();
                    }
                }
            }

            return State.Cleanup;
        }

        private State Cleanup()
        {
            this.TerminateMonitors();

            if (this.debugger != null)
            {
                this.debugger.Dispose();
                this.debugger = null;
            }

            if (this.config.PostRunActions != null)
            {
                this.logger.Info("Running post-run actions...");
                ActionBase.Execute(this.config.PostRunActions);
            }

            if (this.thisRun.Result == TestRunResult.StillRunning)
            {
                this.thisRun.Result = TestRunResult.NothingHappened;
            }

            return State.Stopped;
        }

        private State HandleFailure()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            this.logger.Error("The runner thread has thrown an exception:\r\n{0}", this.failureReason.ToString());
            this.thisRun.Result = TestRunResult.Failed;
            return State.TerminateTarget;
        }

        #endregion

        #region Monitors handling

        private void CreateMonitors()
        {
            if (this.config.ProcessMonitors == null)
                return;

            foreach (MonitorConfigBase configBase in this.config.ProcessMonitors)
            {
                // Still a bit hacky...
                IProcessMonitor monitor = null;
                if (configBase is ProcessIdleMonitorConfig)
                {
                    monitor = new ProcessIdleMonitor(configBase as ProcessIdleMonitorConfig);
                }
                if (configBase is TimeoutMonitorConfig)
                {
                    monitor = new TimeoutMonitor(configBase as TimeoutMonitorConfig);
                }
                if (monitor != null)
                {
                    monitor.KillTargetEvent += new KillTargetEventHandler(this.OnKillProcess);
                    monitor.Start();
                    this.monitors.Add(monitor);
                }
            }
        }

        private void AttachMonitors(uint pid)
        {
            foreach (IProcessMonitor monitor in this.monitors)
            {
                monitor.Attach(pid);
            }
        }

        private void DetachMonitors()
        {
            foreach (IProcessMonitor monitor in this.monitors)
            {
                monitor.Detach();
            }
        }

        private void TerminateMonitors()
        {
            foreach (IProcessMonitor monitor in this.monitors)
            {
                monitor.Stop();
            }
        }

        #endregion

        #region Event handlers

        private void OnProcessCreated(IDebugger debugger, ProcessCreatedEventParams info)
        {
            string processPath = debugger.Processes[info.ProcessId].ImagePath;
            this.logger.Info("Process {0} created ({1}).", info.ProcessId, processPath);
            if (String.IsNullOrEmpty(this.config.ProcessName) || Path.GetFileName(processPath).ToLower() == this.config.ProcessName.ToLower())
            {
                this.targetPid = info.ProcessId;
            }
        }

        private void OnProcessExited(IDebugger debugger, ProcessExitedEventParams info)
        {
            this.logger.Info("Process {0} terminated.", info.ProcessId);
            //if (info.ProcessId == this.targetPid)
            //{
            //    this.state = State.Cleanup;
            //}
            // Wait for all processes to terminate -- accounts for children processes.
            this.logger.Debug("Active processes count: {0}.", this.debugger.Processes.Count);
            if (!this.doneWithTarget)
            {
                this.doneWithTarget = this.debugger.Processes.Count == 0;
            }
        }

        private void OnThreadCreated(IDebugger debugger, ThreadCreatedEventParams info)
        {
            // Spamming the log...
            this.logger.Info("Thread {0}/{1} created.", info.ThreadId, info.ProcessId);
        }

        private void OnThreadExited(IDebugger debugger, ThreadExitedEventParams info)
        {
            // Spamming the log...
            this.logger.Info("Thread {0}/{1} terminated.", info.ThreadId, info.ProcessId);
        }

        private void OnException(IDebugger debugger, ExceptionEventParams info)
        {
            this.logger.Info("Caught an exception {0:X8} at {1:X8} in thread {2}.",
                (uint)info.Info.ExceptionCode,
                (uint)info.Info.OffendingVA,
                info.ThreadId);

            // We care for exceptions on exit -- may be a sign of heap corruption.
            if (this.state != State.MonitorTarget && this.state != State.StopTarget)
            {
                this.logger.Error("Bogus exception -- the target is not being monitored.");
                return;
            }

            if (this.CheckExceptionName(info.Info.ExceptionCode, this.config.IgnoreExceptions))
            {
                this.logger.Info("Passing the exception to the application (always).");
                return;
            }

            if (info.IsFirstChance && this.CheckExceptionName(info.Info.ExceptionCode, this.config.PassExceptions))
            {
                this.logger.Info("Passing the exception to the application (first-chance).");
                return;
            }

            ThreadInfo threadInfo = debugger.Threads[info.ThreadId];
            ProcessInfo processInfo = debugger.Processes[info.ProcessId];

            ExceptionFaultReport report = null;
            if (info.Info is AccessViolationDebugInfo)
            {
                report = new AccessViolationFaultReport();
            }
            else
            {
                report = new ExceptionFaultReport();
            }
            report.Fill(debugger, threadInfo, processInfo, info.Info, report);
            this.thisRun.Result = TestRunResult.ThrewException;
            this.thisRun.ExReport = report;

            this.state = State.TerminateTarget;
        }

        private void OnKillProcess()
        {
            this.logger.Info("A monitor reports the target may be killed now.");
            this.state = this.state != State.StopTarget ? State.StopTarget : State.TerminateTarget;
        }

        #endregion

        #region Various helpers

        private bool CheckExceptionName(EXCEPTION_CODE code, string[] against)
        {
            if (against == null)
                return false;

            foreach (string passedExceptionName in against)
            {
                try
                {
                    EXCEPTION_CODE passedException = (EXCEPTION_CODE)Enum.Parse(typeof(EXCEPTION_CODE), passedExceptionName);
                    if (code == passedException)
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    this.logger.Error("Unrecognized exception name: {0}", passedExceptionName);
                }
            }
            return false;
        }

        #endregion

        #endregion
    }
}
