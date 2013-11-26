
namespace Fuzzman.Agent
{
    public enum TestRunResult
    {
        StillRunning,
        NothingHappened, // Exited by itself
        TimedOut,
        ThrewException,
        Failed, // Failed to run or runner threw an exception.
    }

    public sealed class TestRun
    {
        public TestRunResult Result { get; set; }

        public ExceptionFaultReport ExReport { get; set; }
    }
}
