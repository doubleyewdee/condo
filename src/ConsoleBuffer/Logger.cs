namespace ConsoleBuffer
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class Logger
    {
        private static Logger Instance = null;

        private readonly StreamWriter writer;

        public static void Init(string filename)
        {
            if (Instance == null)
            {
                Instance = new Logger(filename);
            }
        }

        private Logger(string filename)
        {
            this.writer = new StreamWriter(filename, false, Encoding.UTF8);
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
            Instance?.Write(msg);
        }
    }
}
