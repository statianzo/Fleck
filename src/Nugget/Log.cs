using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nugget
{
    [Flags]
    public enum LogLevel 
    { 
        Error,
        Info,
        Debug,
        Waring,
        None,
    }

    public class Log
    {
        static TextWriter _logStream = Console.Out;
        static TextWriter LogStream { get { return _logStream; } set { _logStream = value; } }
        public static LogLevel Level = LogLevel.Error | LogLevel.Debug | LogLevel.Info | LogLevel.Waring;

        public static void Warn(string str)
        {
            LogLine(LogLevel.Waring, String.Format("{0} {1} {2}", DateTime.Now, "WARN", str));
        }

        public static void Error(string str)
        {
            LogLine(LogLevel.Error, String.Format("{0} {1} {2}", DateTime.Now, "ERROR", str));
        }

        public static void Debug(string str)
        {
            LogLine(LogLevel.Debug, String.Format("{0} {1} {2}", DateTime.Now, "DEBUG", str));
        }

        public static void Info(string str)
        {
            LogLine(LogLevel.Info, String.Format("{0} {1} {2}", DateTime.Now, "INFO", str));
        }

        private static void LogLine(LogLevel level, string msg)
        {
            if ((Level & level) == level)
            {
                LogStream.WriteLine(msg);
            }
        }
    }
}
