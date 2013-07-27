using System.Threading;
using Fuzzman.Core.Debugger.Simple;
using Fuzzman.Core.System.Mmap;

namespace Fuzzman.Agent
{
    /// <summary>
    /// The agent application is a simple program responsible for actually performing fuzzing work.
    /// The lifecycle should be as follows:
    /// * Read the provided config file
    /// * Loop:
    /// ** Create a test case directory
    /// ** Open the sample file
    /// ** Apply fuzz algorithms as specified in config file
    /// ** Save the PoC file to the test case directory
    /// ** Run the program under test
    /// ** Observe the program's behavior for crashes
    /// ** Write the report
    /// ** Repeat
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Agent a = new Agent("TestConfig.xml");
            a.Start();
            while (true)
            {
                Thread.Sleep(5000000);
            }
            a.Stop();
        }
    }
}
