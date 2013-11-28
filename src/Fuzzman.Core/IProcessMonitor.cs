
namespace Fuzzman.Core
{
    /// <summary>
    /// Callback to invoke when the monitored process is of no more interest.
    /// </summary>
    public delegate void KillTargetEventHandler();

    /// <summary>
    /// Monitor process-specific activity (e.g. CPU usage, context switches).
    /// </summary>
    public interface IProcessMonitor
    {
        /// <summary>
        /// Begin monitoring.
        /// Perform non-process-specific init tasks here.
        /// </summary>
        void Start();

        /// <summary>
        /// Attach to the target process.
        /// Perform process-specific init tasks here.
        /// </summary>
        /// <param name="pid">Process ID to attach to.</param>
        void Attach(uint pid);

        /// <summary>
        /// Detach from the target process.
        /// </summary>
        void Detach();

        /// <summary>
        /// Wrap things up.
        /// </summary>
        void Stop();

        /// <summary>
        /// Fire this event to cause the target process to terminate.
        /// </summary>
        event KillTargetEventHandler KillTargetEvent;
    }
}
