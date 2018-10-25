namespace ConsoleBuffer
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class Logger
    {
        private static readonly Logger Instance = new Logger();

        private readonly StreamWriter writer;

        private Logger()
        {
            var logfile = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), @"Source\Repos\wincon\wincon.log");
            this.writer = new StreamWriter(logfile, false, Encoding.UTF8);
        }

        private void Write(string msg)
        {
            lock (this.writer)
            {
                this.writer.WriteLine($"{DateTimeOffset.Now}: {msg}");
                this.writer.Flush();
            }
        }

        public static void Verbose(string msg)
        {
            Instance.Write(msg);
        }
    }
}
