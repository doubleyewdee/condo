namespace ConsoleBuffer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;

    public sealed class Buffer : INotifyPropertyChanged
    {
        private readonly List<Line> lines = new List<Line>();
        private readonly object renderLock = new object();

        public short Width { get; set; }
        public short Height { get; set; }

        public Buffer(short width, short height)
        {
            this.Width = width;
            this.Height = height;
            this.lines.Add(new Line());
        }

        public void Append(byte[] bytes, int length)
        {
            lock (this.renderLock)
            {
                int currentLine = this.lines.Count - 1;
                foreach (char ch in Encoding.UTF8.GetString(bytes, 0, length))
                {
                    if (ch == '\n' || this.lines[currentLine].Length == this.Width)
                    {
                        if (currentLine == this.lines.Count - 1)
                        {
                            this.lines.Add(new Line());
                            ++currentLine;
                        }

                        if (ch == '\n')
                            continue;
                    }

                    this.lines[currentLine].Append(new Character { Glyph = ch });
                    if (ch == ' ')
                        Logger.Verbose("space!");
                }
            }
        }

        /// <summary>
        /// Render character-by-character onto the specified target.
        /// </summary>
        public void Render(IRenderTarget target)
        {
            lock (this.renderLock)
            {
                var startLine = Math.Max(0, this.lines.Count - this.Height);
                for (var x = 0; x < this.Height; ++x)
                {
                    var renderLine = startLine + x;
                    var line = renderLine < this.lines.Count ? this.lines[renderLine] : Line.Empty;
                    short y = 0;
                    foreach (var c in line)
                    {
                        target.RenderCharacter(c, x, y);
                        ++y;
                    }
                    while (y < this.Width - 1)
                    {
                        target.RenderCharacter(new Character { Glyph = ' ' }, x, y);
                        ++y;
                    }
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
