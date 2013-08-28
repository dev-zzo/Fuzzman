using System.Xml.Serialization;
using Fuzzman.Agent.Actions;
using Fuzzman.Core.Monitor;

namespace Fuzzman.Agent.Config
{
    [XmlType(TypeName = "Agent")]
    public class AgentConfiguration
    {
        public AgentConfiguration()
        {
            this.TestCasesPath = "";
            this.TestCaseTemplate = "{DATETIME}-TC{TCN}";
            this.Timeout = 30;
            this.RunCount = 10;
            this.DisableDebugHeap = true;
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
        /// Target timeout, in seconds.
        /// </summary>
        public int Timeout { get; set; }

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

        public ProcessIdleMonitorConfig ProcessIdleMonitor { get; set; }

        [XmlArrayItem("DeleteRegistryKey", Type = typeof(DeleteRegistryKeyAction))]
        [XmlArrayItem("DeleteRegistryValue", Type = typeof(DeleteRegistryValueAction))]
        [XmlArrayItem("DeleteFile", Type = typeof(DeleteFileAction))]
        [XmlArrayItem("DeleteFolder", Type = typeof(DeleteFolderAction))]
        public ActionBase[] PostRunActions { get; set; }
    }
}
