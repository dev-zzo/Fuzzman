using System.Threading;
using Fuzzman.Core.Debugger.Simple;

namespace Fuzzman.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleDebugger d = new SimpleDebugger();
            d.StartTarget("CrashApp.exe");
            Thread.Sleep(100000);
            d.Stop();
        }
    }
}
