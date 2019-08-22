namespace ConsoleBuffer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public sealed class Logger
    {
        public enum Level
        {
            Error = 0,
            Warning,
            Info,
            Verbose,
            Debug,
        }

        private static Logger Instance = null;
        private static Level TraceLevel =
#if DEBUG
            Level.Debug;
#else
            Level.Warning;
#endif

        private readonly StreamWriter writer;
        private readonly Level minLevel;

        public static void Init(string filename, Level level)
        {
            if (Instance == null)
            {
                Instance = new Logger(filename, level);
            }
        }

        private Logger(string filename, Level level)
        {
            this.writer = new StreamWriter(filename, false, Encoding.UTF8);
            this.minLevel = level;
        }

        private string Write(FormattableString msg, Level level)
        {
            if ((int)this.minLevel < (int)level) return null;

            var formatted = FormatMessage(msg, level);
            lock (this.writer)
            {
                this.writer.WriteLine(formatted);
                this.writer.Flush();
            }

            return formatted;
        }

        private static string LevelToString(Level level)
        {
            switch (level)
            {
            case Level.Error:
                return "err";
            case Level.Warning:
                return "warn";
            case Level.Info:
                return "info";
            case Level.Verbose:
                return "spam";
            case Level.Debug:
                return "debug";
            default:
                return "idk?";
            }
        }

        private static string FormatMessage(FormattableString msg, Level level)
        {
            return $"[{DateTimeOffset.Now:o} {LevelToString(level)}]: {msg}";
        }

        public static void Log(FormattableString msg, Level level)
        {
            var str = Instance?.Write(msg, level);

            if ((int)TraceLevel >= (int)level) 
                Trace.WriteLine(str ?? FormatMessage(msg, level));
        }

        public static void Error(FormattableString msg) { Log(msg, Level.Error); }
        public static void Warning(FormattableString msg) { Log(msg, Level.Warning); }
        public static void Info(FormattableString msg) { Log(msg, Level.Info); }
        public static void Verbose(FormattableString msg) { Log(msg, Level.Verbose); }
        public static void Debug(FormattableString msg) { Log(msg, Level.Debug); }
    }
}
