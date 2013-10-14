using System;
using System.IO;

namespace Fuzzman.Core
{
    public enum LogLevel
    {
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
    }

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
            this.minLevel = LogLevel.INFO;
            this.path = path;
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            this.stream = new FileStream(this.path, FileMode.Append);
            this.writer = new StreamWriter(this.stream);
        }

        public void SetLevel(LogLevel level)
        {
            this.minLevel = level;
        }

        public void Debug(string message)
        {
            this.Write(LogLevel.DEBUG, message);
        }

        public void Debug(string format, params object[] args)
        {
            this.Debug(String.Format(format, args));
        }

        public void Info(string message)
        {
            this.Write(LogLevel.INFO, message);
        }

        public void Info(string format, params object[] args)
        {
            this.Info(String.Format(format, args));
        }

        public void Warning(string message)
        {
            this.Write(LogLevel.WARN, message);
        }

        public void Warning(string format, params object[] args)
        {
            this.Warning(String.Format(format, args));
        }

        public void Error(string message)
        {
            this.Write(LogLevel.ERROR, message);
        }

        public void Error(string format, params object[] args)
        {
            this.Error(String.Format(format, args));
        }

        public void Fatal(string message)
        {
            this.Write(LogLevel.FATAL, message);
        }

        public void Fatal(string format, params object[] args)
        {
            this.Fatal(String.Format(format, args));
        }

        private LogLevel minLevel;
        private string path;
        private FileStream stream;
        private StreamWriter writer;

        private void Write(LogLevel level, string message)
        {
            if (level > this.minLevel)
                return;

            string line = String.Format("[{0,5}] {1}", level.ToString(), message);
            lock (this.writer)
            {
                writer.WriteLine(line);
                writer.Flush();
                Console.WriteLine(line);
            }
        }
    }
}
