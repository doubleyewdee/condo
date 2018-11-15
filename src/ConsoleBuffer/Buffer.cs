namespace ConsoleBuffer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Text;

    public sealed class Buffer : INotifyPropertyChanged
    {
        private readonly SequenceParser parser = new SequenceParser();
        private readonly List<Line> lines = new List<Line>();
        private readonly object renderLock = new object();

        private short cursorX;
        private short cursorY;
        private int currentChar;
        public (short X, short Y) CursorPosition => (this.cursorX, this.cursorY);

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
                for (var i = 0;i < length; ++i)
                {
                    if (!this.AppendChar(bytes[i])) continue;

                    switch (this.parser.Append(this.currentChar))
                    {
                    case ParserAppendResult.Render:
                        this.lines[this.currentLine].Set(this.cursorX, new Character { Glyph = this.currentChar });
                        this.cursorX = (short)Math.Min(this.Width - 1, this.cursorX + 1);
                        break;
                    case ParserAppendResult.Complete:
                        this.ExecuteParserCommand();
                        break;
                    case ParserAppendResult.Pending:
                        break;
                    case ParserAppendResult.Invalid:
                        // XXX: we should keep a trailing history of received bytes or something so we can actually log meaningful data.
                        Logger.Verbose("Invalid command sequence in parser.");
                        break;
                    default:
                        throw new InvalidOperationException("unexpected parser result");
                    }
                }
            }
        }

        /// <summary>
        /// Append a single byte to the current character.
        /// </summary>
        /// <returns>true if the current character represents a completed Unicode character</returns>
        private bool AppendChar(byte b)
        {
            // TODO: actual utf-8 parsing.
            this.currentChar = b;
            return true;
        }

        private void ExecuteParserCommand()
        {
            switch (this.parser.Command)
            {
            case ControlCharacterCommand ctrl:
                this.HandleControlCharacter(ctrl.Code);
                break;
            case UnsupportedCommand unsupported:
                // XXX: would be nice to log the sequence
                Logger.Verbose("Unsupported command provided.");
                break;
            default:
                throw new InvalidOperationException("Unknown command type passed.");
            }
        }

        private void HandleControlCharacter(ControlCharacterCommand.ControlCode code)
        {
            switch (code)
            {
            case ControlCharacterCommand.ControlCode.NUL:
                // XXX: do we want to print these in some magic way? it seems like most terminals just discard these characters when received.
                break;
            case ControlCharacterCommand.ControlCode.BEL:
                // XXX: need to raise a beep event.
                break;
            case ControlCharacterCommand.ControlCode.BS:
                this.cursorX = (short)Math.Max(0, this.cursorX - 1);
                break;
            case ControlCharacterCommand.ControlCode.CR:
                this.cursorX = 0;
                break;
            case ControlCharacterCommand.ControlCode.FF: // NB: could clear screen with this if we were so inclined. apparently xterm treats this as LF though, let's emulate.
            case ControlCharacterCommand.ControlCode.LF:
                if (this.currentLine == this.lines.Count - 1)
                {
                    this.lines.Add(new Line());
                }

                this.cursorY = (short)Math.Min(this.Height - 1, this.cursorY + 1);
                break;
            case ControlCharacterCommand.ControlCode.TAB:
                // XXX: we don't handle commands to set tab stops yet but I guess need to do so at some point!
                this.cursorX = (short)Math.Max(this.Width - 1, (this.cursorX + 8 - (this.cursorX % 8)));
                break;
            default:
                // XXX: should log the sequence.
                Logger.Verbose("Encountered unsupported sequence.");
                break;
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
