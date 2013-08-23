
namespace Fuzzman.Core
{
    public delegate void KillTargetEventHandler();

    public interface IMonitor
    {
        void Start();

        void Attach(uint pid);

        void Detach();

        void Stop();

        event KillTargetEventHandler KillTargetEvent;
    }
}
