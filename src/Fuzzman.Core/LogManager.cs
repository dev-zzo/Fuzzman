using System;
using System.IO;

namespace Fuzzman.Core
{
    /// <summary>
    /// Manages the access to logging facility.
    /// </summary>
    public class LogManager
    {
        public static void Initialize(string path)
        {
            instance = new LameFileLogger(path);
        }

        public static ILogger GetLogger(string facility)
        {
            return instance;
        }

        public static ILogger GetLogger()
        {
            return instance;
        }

        private static ILogger instance;
    }

    class LameFileLogger : ILogger
    {
        public LameFileLogger(string path)
        {
            this.path = path;
            this.stream = new FileStream(this.path, FileMode.Append);
            this.writer = new StreamWriter(this.stream);
        }

        public void Debug(string message)
        {
            this.Write("DEBUG", message);
        }

        public void Debug(string format, params object[] args)
        {
            this.Debug(String.Format(format, args));
        }

        public void Info(string message)
        {
            this.Write("INFO", message);
        }

        public void Info(string format, params object[] args)
        {
            this.Info(String.Format(format, args));
        }

        public void Warning(string message)
        {
            this.Write("WARN", message);
        }

        public void Warning(string format, params object[] args)
        {
            this.Warning(String.Format(format, args));
        }

        public void Error(string message)
        {
            this.Write("ERROR", message);
        }

        public void Error(string format, params object[] args)
        {
            this.Error(String.Format(format, args));
        }

        public void Fatal(string message)
        {
            this.Write("FATAL", message);
        }

        public void Fatal(string format, params object[] args)
        {
            this.Fatal(String.Format(format, args));
        }

        private string path;
        private FileStream stream;
        private StreamWriter writer;

        private void Write(string level, string message)
        {
            string line = String.Format("[{0,5}] {1}", level, message);
            lock (this.writer)
            {
                writer.WriteLine(line);
                writer.Flush();
                Console.WriteLine(line);
            }
        }
    }
}
