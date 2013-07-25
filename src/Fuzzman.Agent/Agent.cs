using System.Xml.Serialization;
using Fuzzman.Core;
using System.IO;
using Fuzzman.Agent.Config;
using Fuzzman.Core.System.Mmap;

namespace Fuzzman.Agent
{
    public class Agent : IAgent
    {
        public Agent(string configPath)
        {
            this.config = LoadConfig(configPath);
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        private Configuration config = null;

        private static Configuration LoadConfig(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return (Configuration)serializer.Deserialize(stream);
            }
        }

        private static string SaveConfig(Configuration config)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            StringWriter writer = new StringWriter();
            serializer.Serialize(writer, config);
            return writer.ToString();
        }

        private void Iteration()
        {
        }
    }
}
