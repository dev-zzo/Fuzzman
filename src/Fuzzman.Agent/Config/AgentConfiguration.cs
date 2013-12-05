using System.Xml.Serialization;
using Fuzzman.Agent.Actions;
using Fuzzman.Core.Monitor;
using Fuzzman.Core.Debugger;
using Fuzzman.Core;

namespace Fuzzman.Agent.Config
{
    [XmlType(TypeName = "Agent")]
    public class AgentConfiguration
    {
        public AgentConfiguration()
        {
            this.TestCasesPath = "";
            this.TestCaseTemplate = "{DATETIME}-TC{TCN}";
            this.RunCount = 10;
            this.DisableDebugHeap = true;
            this.FuzzerType = "DumbFuzzer";
        }

        /// <summary>
        /// What to execute as a test subject.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// Which process to watch for (exe name with extension).
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// How many times to run the test case, if the first run raised an exception.
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Where to get the source files to be fuzzed.
        /// </summary>
        [XmlArrayItem("SourceFilePath")]
        public string[] Sources { get; set; }

        /// <summary>
        /// Which exceptions are to be passed to the application on the first chance.
        /// </summary>
        [XmlArrayItem("Exception")]
        public string[] PassExceptions { get; set; }

        /// <summary>
        /// Which exceptions are to be passed to the application.
        /// </summary>
        [XmlArrayItem("Exception")]
        public string[] IgnoreExceptions { get; set; }

        /// <summary>
        /// Which exception locations to ignore.
        /// </summary>
        [XmlArrayItem("Location")]
        public Location[] IgnoreLocations { get; set; }

        /// <summary>
        /// Where to keep the test cases.
        /// </summary>
        public string TestCasesPath { get; set; }

        /// <summary>
        /// Template of a dir name in testcases dir.
        /// </summary>
        public string TestCaseTemplate { get; set; }

        /// <summary>
        /// Whether we should disable the debug heap.
        /// Defaults to true -- gets much close to free runs.
        /// </summary>
        public bool DisableDebugHeap { get; set; }

        /// <summary>
        /// Whether the target is a console app.
        /// No attempt to gracefully close a console app will be done.
        /// </summary>
        public bool IsConsoleApp { get; set; }

        /// <summary>
        /// Which fuzzer to use.
        /// </summary>
        public string FuzzerType { get; set; }

        /// <summary>
        /// Process monitors to be instantiated.
        /// </summary>
        [XmlArrayItem("ProcessIdleMonitor", Type = typeof(ProcessIdleMonitorConfig))]
        [XmlArrayItem("TimeoutMonitor", Type = typeof(TimeoutMonitorConfig))]
        public MonitorConfigBase[] ProcessMonitors { get; set; }

        /// <summary>
        /// Global monitors to be instantiated.
        /// </summary>
        [XmlArrayItem("PopupMonitor", Type = typeof(PopupMonitorConfig))]
        public MonitorConfigBase[] GlobalMonitors { get; set; }

        /// <summary>
        /// Actions to perform when the target process is killed.
        /// </summary>
        [XmlArrayItem("DeleteRegistryKey", Type = typeof(DeleteRegistryKeyAction))]
        [XmlArrayItem("DeleteRegistryValue", Type = typeof(DeleteRegistryValueAction))]
        [XmlArrayItem("DeleteFile", Type = typeof(DeleteFileAction))]
        [XmlArrayItem("DeleteFolder", Type = typeof(DeleteFolderAction))]
        public ActionBase[] PostRunActions { get; set; }
    }
}
