using CommandLine;

namespace Fuzzman.Agent.Config
{
    public class Options
    {
        [Option('c', "config", Required = true, HelpText = "XML configration file.")]
        public string XmlConfigPath { get; set; }

        [Option('s', "skip", DefaultValue = 0, HelpText = "Iterations to skip.")]
        public int SkipIterations { get; set; }

        [Option('r', "random-seed", DefaultValue = 0, HelpText = "A seed for the RNG.")]
        public int RandomSeed { get; set; }
    }
}
