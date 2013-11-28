using System.Xml.Serialization;

namespace Fuzzman.Core.Monitor
{
    [XmlType(TypeName = "TimeoutMonitor")]
    public class TimeoutMonitorConfig : MonitorConfigBase
    {
        public TimeoutMonitorConfig()
        {
            this.Interval = 10;
        }

        public int Interval { get; set; }
    }
}
