using System.Xml.Serialization;

namespace Fuzzman.Core.Monitor
{
    [XmlType(TypeName = "TimeoutMonitor")]
    public class TimeoutMonitorConfig
    {
        public TimeoutMonitorConfig()
        {
            this.Interval = 10;
        }

        public int Interval { get; set; }
    }
}
