
namespace Fuzzman.Core
{
    /// <summary>
    /// Generic logging interface.
    /// Should be implemented by whatever underlying system I choose.
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Debug(string format, params object[] args);

        void Info(string message);
        void Info(string format, params object[] args);

        void Warning(string message);
        void Warning(string format, params object[] args);

        void Error(string message);
        void Error(string format, params object[] args);

        void Fatal(string message);
        void Fatal(string format, params object[] args);
    }
}
