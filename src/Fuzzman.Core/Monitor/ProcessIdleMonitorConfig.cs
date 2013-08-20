using System.Xml.Serialization;

namespace Fuzzman.Core.Monitor
{
    [XmlType(TypeName = "ProcessIdleMonitor")]
    public class ProcessIdleMonitorConfig
    {
        public ProcessIdleMonitorConfig()
        {
            this.PollInterval = 250;
            this.MaxIdleCount = 10;
            this.CheckTimes = false;
            this.CheckContextSwitches = false;
        }

        public int PollInterval { get; set; }

        public int MaxIdleCount { get; set; }

        public bool CheckTimes { get; set; }

        public bool CheckContextSwitches { get; set; }
    }
}
