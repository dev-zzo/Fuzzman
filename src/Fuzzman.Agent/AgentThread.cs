using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Fuzzman.Agent.Config;
using Fuzzman.Agent.Fuzzers;
using Fuzzman.Core;
using Fuzzman.Core.Debugger;
using Fuzzman.Core.Monitor;
using Fuzzman.Core.Platform.Mmap;
using System.Reflection;

namespace Fuzzman.Agent
{
    internal class AgentThread
    {
        public AgentThread(int id, AgentConfiguration config)
        {
            this.workerId = id;
            this.config = config;
        }

        public AgentThread(int id, AgentConfiguration config, int seed)
        {
            this.workerId = id;
            this.config = config;
            this.startSeed = seed;
        }

        public void Start()
        {
            this.thread = new Thread(this.ThreadProc);
            this.thread.Priority = ThreadPriority.Highest;
            this.thread.Start();
        }

        public void Stop()
        {
            this.aborting = true;
            // TOCTOU race.
            if (this.runner != null)
            {
                this.runner.AbortTestCase();
            }
        }

        public void Join()
        {
            if (this.thread != null && this.thread.IsAlive)
            {
                this.logger.Info("Waiting for agent thread...");
                this.thread.Join();
            }
        }

        private readonly ILogger logger = LogManager.GetLogger();
        private readonly int workerId;
        private readonly AgentConfiguration config;
        private readonly List<IGlobalMonitor> monitors = new List<IGlobalMonitor>();
        private int startSeed;
        private Thread thread;
        private bool aborting;
        private Runner runner;
        private TestCase testCase;

        private void ThreadProc()
        {
            this.CreateMonitors();

            Assembly asm = Assembly.GetExecutingAssembly();
            Type fuzzerType = asm.GetType("Fuzzman.Agent.Fuzzers." + this.config.FuzzerType);

            try
            {
                while (!this.aborting)
                {
                    int seed;
                    if (this.startSeed == 0)
                    {
                        int newSeed = new Random().Next();
                        seed = newSeed + this.workerId * 1777;
                    }
                    else
                    {
                        seed = this.startSeed;
                        this.startSeed = 0;
                    }
                    IRandom rng = new StdRandom(seed);

                    IFuzzer fuzzer = (IFuzzer)Activator.CreateInstance(fuzzerType, new object[] { rng });

                    // Build a new test case.
                    this.testCase = new TestCase();

                    this.logger.Fatal("[{0}] *** Test case #{1} setting up (seed: {2}).", this.workerId, this.testCase.TestCaseNumber, seed);

                    uint sourceIndex = rng.GetNext(0, (uint)this.config.Sources.Length);
                    string currentSource = this.config.Sources[sourceIndex];
                    this.logger.Info("[{0}] Using source: {1}", this.workerId, currentSource);
                    fuzzer.Populate(currentSource);

                    string workId = String.Format("probe.{0}", this.workerId);
                    string workingDirectory = Path.Combine(this.config.TestCasesPath, workId);
                    Directory.CreateDirectory(workingDirectory);
                    ILogger runnerLogger = LogManager.GetLogger(Path.Combine(workId, "fuzzman.log"));

                    string sampleFileName = Path.GetFileName(currentSource);
                    string samplePath = Path.Combine(workingDirectory, sampleFileName);

                    // Run target for probe.
                    this.logger.Info("[{0}] Running a probe run.", this.workerId);
                    fuzzer.Apply(currentSource, samplePath);
                    string commandLine = this.BuildCommandLine(samplePath);
                    this.runner = new Runner(this.config, runnerLogger);
                    TestRun testResult = this.runner.RunTestCase(commandLine);
                    if (this.aborting)
                        break;

                    if (this.IsValidResult(testResult))
                    {
                        // Repeat runs.
                        this.logger.Info("[{0}] Running statistical runs.", this.workerId);
                        while (this.testCase.RunCount < this.config.RunCount && !this.aborting)
                        {
                            this.testCase.RunCount++;

                            this.runner = new Runner(this.config, runnerLogger);
                            testResult = this.runner.RunTestCase(commandLine);

                            if (this.IsValidResult(testResult))
                            {
                                this.testCase.Reports.Add(testResult.ExReport);
                            }
                        }

                        TestCaseAnalyser analyser = new TestCaseAnalyser(this.testCase);
                        analyser.Analyse();
                        if (analyser.IsInteresting)
                        {
                            this.logger.Info("[{0}] Test case produced some results.", this.workerId);
                            using (FileStream stream = new FileStream(Path.Combine(workingDirectory, "analysis.txt"), FileMode.CreateNew, FileAccess.Write))
                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                writer.WriteLine(analyser.ReportText);
                            }

                            StringBuilder builder = new StringBuilder(this.config.TestCaseTemplate, 256);
                            builder.Replace("{DATETIME}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                            builder.Replace("{SUMMARY}", analyser.ReportSummary);

                            // Try to minimize the reproducer.
                            if (this.testCase.Reports.Count == this.testCase.RunCount)
                            {
                                this.logger.Info("[{0}] Test case looks stable; starting minimization runs.", this.workerId);
                                string minimalSampleName = "minimal" + Path.GetExtension(currentSource);
                                string minimalSamplePath = Path.Combine(workingDirectory, minimalSampleName);

                                for (int i = 0; i < fuzzer.Diffs.Length; ++i)
                                {
                                    fuzzer.Diffs[i].Ignored = true;
                                    fuzzer.Apply(currentSource, minimalSamplePath);

                                    this.runner = new Runner(this.config, this.logger);
                                    testResult = this.runner.RunTestCase(BuildCommandLine(minimalSamplePath));

                                    if (!this.IsValidResult(testResult))
                                    {
                                        fuzzer.Diffs[i].Ignored = false;
                                    }
                                }
                                this.logger.Info("[{0}] Minimization runs complete.", this.workerId);
                            }

                            TryMoveDirectory(workingDirectory, Path.Combine(this.config.TestCasesPath, builder.ToString()));
                        }
                    }
                    else
                    {
                        this.logger.Info("[{0}] Nothing interesting.", this.workerId);
                    }

                    // Cleanup
                    TryDeleteDirectory(workingDirectory);
                }
            }
            catch (Exception ex)
            {
            }

            this.TerminateMonitors();
        }

        private string BuildCommandLine(string samplePath)
        {
            StringBuilder builder = new StringBuilder(this.config.CommandLine, 256);
            builder.Replace("{TARGET}", samplePath);
            return builder.ToString();
        }

        private bool IsValidResult(TestRun testResult)
        {
            return testResult.Result == TestRunResult.ThrewException && !CheckExceptionLocation(testResult.ExReport.Location, this.config.IgnoreLocations);
        }

        #region Monitors handling

        private void CreateMonitors()
        {
            if (this.config.GlobalMonitors == null)
                return;

            foreach (MonitorConfigBase configBase in this.config.GlobalMonitors)
            {
                // Still a bit hacky...
                IGlobalMonitor monitor = null;
                if (configBase is PopupMonitorConfig)
                {
                    monitor = new PopupMonitor(configBase as PopupMonitorConfig);
                }
                if (monitor != null)
                {
                    monitor.Start();
                    this.monitors.Add(monitor);
                }
            }
        }

        private void TerminateMonitors()
        {
            foreach (IGlobalMonitor monitor in this.monitors)
            {
                monitor.Stop();
            }
        }

        #endregion

        private bool CheckExceptionLocation(Location thisLocation, Location[] against)
        {
            if (against == null)
                return false;

            foreach (Location location in against)
            {
                if (location == thisLocation)
                    return true;
            }

            return false;
        }

        private static void TryMoveDirectory(string source, string target)
        {
            if (Directory.Exists(source))
            {
                int retryCount = 10;
                do
                {
                    try
                    {
                        Directory.Move(source, target);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Bah! Failed to move the working directory!");
                        --retryCount;
                        Thread.Sleep(2500);
                    }
                } while (retryCount > 0);
            }
        }

        private static void TryDeleteDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                int retryCount = 10;
                do
                {
                    try
                    {
                        Directory.Delete(dir, true);
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

    }
}
