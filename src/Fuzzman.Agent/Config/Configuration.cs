using System.Xml.Serialization;
using System.IO;
using Fuzzman.Core;

namespace Fuzzman.Agent.Config
{
    [XmlRoot(ElementName = "Fuzzman")]
    public class Configuration
    {
        public AgentConfiguration Agent { get; set; }

        public string LogFilePath { get; set; }

        public string LogLevel { get; set; } 

        public static Configuration LoadConfig(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return (Configuration)serializer.Deserialize(stream);
            }
        }
    }
}
