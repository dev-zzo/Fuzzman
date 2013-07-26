using System.Xml.Serialization;

namespace Fuzzman.Agent.Config
{
    [XmlRoot(ElementName = "Fuzzman")]
    public class Configuration
    {
        public AgentConfiguration Agent { get; set; }

        public string LogFilePath { get; set; }
    }
}
