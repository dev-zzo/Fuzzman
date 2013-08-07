
namespace Fuzzman.Core
{
    /// <summary>
    /// Generic logging interface.
    /// Should be implemented by whatever underlying system I choose.
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);

        void Info(string message);
        void Info(string format, params object[] args);

        void Warning(string message);

        void Error(string message);

        void Fatal(string message);
        void Fatal(string format, params object[] args);
    }
}
