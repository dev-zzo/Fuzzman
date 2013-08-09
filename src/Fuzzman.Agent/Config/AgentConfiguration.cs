using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
            this.RerunCount = 9;
        }

        /// <summary>
        /// What to execute as a test subject.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// Target timeout, in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// How many times to rerun the test case, if the first run raised an exception.
        /// </summary>
        public int RerunCount { get; set; }

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
    }
}
