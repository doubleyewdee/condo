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

        public (short X, short Y) CursorPosition { get; private set; }

        private short bufferTopVisibleLine
        {
            get
            {
                return (short)Math.Max(0, this.lines.Count - this.Height);
            }
        }
        private short currentLine
        {
            get
            {
                return (short)(this.bufferTopVisibleLine + this.CursorPosition.Y);
            }
        }

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
                foreach (char ch in Encoding.UTF8.GetString(bytes, 0, length))
                {
                    if (ch == '\n')
                    {
                        Logger.Verbose($"newline (current: {this.lines[this.currentLine]}!");
                        if (this.currentLine == this.lines.Count - 1)
                        {
                            this.lines.Add(new Line());
                        }

                        this.CursorPosition = (X: this.CursorPosition.X, Y: (short)Math.Min(this.Height - 1, this.CursorPosition.Y + 1));
                    }

                    this.lines[this.currentLine].Append(new Character { Glyph = ch });
                    this.CursorPosition = (X: (short)Math.Min(this.Width - 1, this.CursorPosition.X + 1), this.CursorPosition.Y);
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
                for (var x = 0; x < this.Height; ++x)
                {
                    var renderLine = this.bufferTopVisibleLine + x;
                    var line = renderLine < this.lines.Count ? this.lines[renderLine] : Line.Empty;
                    short y = 0;
                    foreach (var c in line)
                    {
                        target.RenderCharacter(c, x, y);
                        ++y;
                    }
                    while (y < this.Width)
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
