using System;

namespace Fleck
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public class FleckLog
    {
        public static LogLevel Level = LogLevel.Info;

        public static Action<LogLevel, string, Exception> LogAction = (level, message, ex) 
            => Console.WriteLine("{0} [{1}] {2} {3}", DateTime.Now, level, message, ex);

        public static void Warn(string message, Exception ex = null)
        {
            if(ShouldLog(LogLevel.Warn))
                LogAction(LogLevel.Warn, message, ex);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ShouldLog(LogLevel.Error))
                LogAction(LogLevel.Error, message, ex);
        }

        public static void Debug(string message, Exception ex = null)
        {
            if (ShouldLog(LogLevel.Debug))
                LogAction(LogLevel.Debug, message, ex);
        }

        public static void Info(string message, Exception ex = null)
        {
            if (ShouldLog(LogLevel.Info))
                LogAction(LogLevel.Info, message, ex);
        }

        protected static bool ShouldLog(LogLevel level)
        {
            return (level >= Level);
        }
    }
}
