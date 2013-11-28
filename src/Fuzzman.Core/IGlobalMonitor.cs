
namespace Fuzzman.Core
{
    /// <summary>
    /// A monitor that performs monitoring activities not specific to a process.
    /// A good example is watching for popup windows.
    /// </summary>
    public interface IGlobalMonitor
    {
        /// <summary>
        /// Start monitoring.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop monitoring.
        /// </summary>
        void Stop();
    }
}
