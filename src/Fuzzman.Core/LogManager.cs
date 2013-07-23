using System;
using System.IO;

namespace Fuzzman.Core
{
    /// <summary>
    /// Manages the access to logging facility.
    /// </summary>
    public class LogManager
    {
        public static ILogger GetLogger(string facility)
        {
            return instance;
        }

        public static ILogger GetLogger()
        {
            return instance;
        }

        private static ILogger instance = new LameFileLogger("logfile.txt");
    }

    class LameFileLogger : ILogger
    {
        public LameFileLogger(string path)
        {
            this.path = path;
        }

        public void Debug(string message)
        {
            this.Write("DEBUG", message);
        }

        public void Info(string message)
        {
            this.Write("INFO", message);
        }

        public void Warning(string message)
        {
            this.Write("WARNING", message);
        }

        public void Error(string message)
        {
            this.Write("ERROR", message);
        }

        public void Fatal(string message)
        {
            this.Write("FATAL", message);
        }

        private string path;

        private void Write(string level, string message)
        {
            using (FileStream stream = new FileStream(this.path, FileMode.Append))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine(String.Format("[{0,5}] {1}", level, message));
                }
            }
        }
    }
}
