using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fuzzman.OS.Windows.Debuggers.Simple;
using System.Threading;

namespace Fuzzman.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleDebugger d = new SimpleDebugger();
            d.CreateProcess("cmd.exe");
            Thread.Sleep(100000);
            d.Stop();
        }
    }
}
