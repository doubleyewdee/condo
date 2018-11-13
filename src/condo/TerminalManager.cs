namespace condo
{
    using ConsoleBuffer;
    using System.Collections.Generic;

    /// <summary>
    /// Manages creating/destroying terminals through queued requests.
    /// </summary>
    public sealed class TerminalManager
    {
        public static readonly TerminalManager Instance = new TerminalManager();

        private readonly object terminalLock = new object();
        private readonly Dictionary<int, ConsoleWrapper> terminals;

        private TerminalManager()
        {
            this.terminals = new Dictionary<int, ConsoleWrapper>();
        }

        public ConsoleWrapper GetOrCreate(int id, string command)
        {
            lock (this.terminalLock)
            {
                if (this.terminals.TryGetValue(id, out ConsoleWrapper con))
                {
                    return con;
                }

                con = new ConsoleWrapper(command);
                this.terminals[id] = con;
                return con;
            }
        }
    }
}
