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
            this.TestCaseTemplate = "TC{id}";
        }

        /// <summary>
        /// What to execute as a test subject.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string CommandLine { get; set; }

        /// <summary>
        /// Where to get the source file to be fuzzed.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string SourceFilePath { get; set; }

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
