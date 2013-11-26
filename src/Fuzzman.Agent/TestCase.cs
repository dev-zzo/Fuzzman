using System.Collections.Generic;
using System.Threading;

namespace Fuzzman.Agent
{
    public class TestCase
    {
        public TestCase()
        {
            this.TestCaseNumber = Interlocked.Increment(ref nextTestCaseNumber);
        }

        /// <summary>
        /// Unique test case ID in this test run.
        /// </summary>
        public int TestCaseNumber { get; private set; }

        /// <summary>
        /// How many times the test case has been run.
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Reports associated with this test case.
        /// </summary>
        public IList<FaultReport> Reports { get { return this.reports; } }

        private static int nextTestCaseNumber = 0;
        private readonly IList<FaultReport> reports = new List<FaultReport>();
    }
}
