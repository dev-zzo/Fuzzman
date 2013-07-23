using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Fuzzman.Core.Debugger.Simple;

namespace Fuzzman.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleDebugger d = new SimpleDebugger();
            d.CreateTarget("cmd.exe");
            Thread.Sleep(100000);
            d.Stop();
        }
    }
}
