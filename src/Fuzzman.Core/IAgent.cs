using System;

namespace Fuzzman.Core
{
    /// <summary>
    /// A remote agent.
    /// This is a controlling and reporting entity ran on the test machines.
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Start the agent activity.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the agent activity.
        /// </summary>
        void Stop();
    }
}
