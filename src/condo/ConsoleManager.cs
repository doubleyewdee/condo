namespace condo
{
    using ConsoleBuffer;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manages creating/destroying terminals through queued requests.
    /// </summary>
    public sealed class ConsoleManager
    {
        private readonly object consolesLock = new object();
        private readonly List<ConsoleWrapper> consoles = new List<ConsoleWrapper>();

        /// <summary>
        /// Create a new console wrapper.
        /// </summary>
        /// <param name="command">Command line to wrap.</param>
        /// <returns>The id and associated wrapper for the console.</returns>
        public (int Id, ConsoleWrapper ConsoleWrapper) Create(string command)
        {
            lock (this.consolesLock)
            {
                var wrapper = new ConsoleWrapper(command);
                this.consoles.Add(wrapper);
                return (this.consoles.Count - 1, wrapper);
            }
        }

        /// <summary>
        /// Access the console at the given index.
        /// </summary>
        /// <param name="id">Index to access.</param>
        public ConsoleWrapper this[int id]
        {
            get
            {
                lock (this.consolesLock)
                {
                    if (id < 0 || id >= this.consoles.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(id));
                    }

                    return this.consoles[id];
                }
            }
        }
    }
}
