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
            this.writer = new StreamWriter(@"C:\Users\wd\Source\Repos\wincon\wincon.log", false, Encoding.UTF8);
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
