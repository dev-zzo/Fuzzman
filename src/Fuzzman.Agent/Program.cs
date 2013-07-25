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
            MappedFile f = new MappedFile("CrashApp.exe", System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            MappedFileView v = f.CreateView(0, 1024);
            uint mz;
            v.Read(0, out mz);

            Agent a = new Agent("testtest");

            SimpleDebugger d = new SimpleDebugger();
            d.StartTarget("CrashApp.exe");
            Thread.Sleep(100000);
            d.Stop();
        }
    }
}
