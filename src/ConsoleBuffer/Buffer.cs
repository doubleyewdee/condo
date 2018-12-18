namespace ConsoleBuffer
{
    using System;
    using System.ComponentModel;
    using System.Text;

    public sealed class Buffer : INotifyPropertyChanged
    {
        private const int MinimumWidth = 80;
        private const int MinimumHeight = 25;

        private readonly SequenceParser parser = new SequenceParser();
        private readonly CircularBuffer<Line> lines = new CircularBuffer<Line>(short.MaxValue);
        private readonly object renderLock = new object();

        private short cursorX;
        private short cursorY;
        private short MaxCursorX => (short)(this.Width - 1);
        private short MaxCursorY => (short)(this.Height - 1);

        private long receivedCharacters;
        private long wrapCharacter;
        private int currentChar;
        private Character characterTemplate = new Character { Glyph = 0x0 };

        /// <summary>
        /// The viewable dimensions of the buffer.
        /// </summary>
        public (int X, int Y) ViewDimensions => (this.Width, this.Height);

        /// <summary>
        /// we store X/Y as 0-offset indexes for convenience. escape codes will pass these around as 1-offset (top left is 1,1)
        /// and we'll translate that nonsense where we have to.
        /// </summary>
        public (short X, short Y) CursorPosition => (this.cursorX, this.cursorY);
        /// <summary>
        /// True if the cursor is currently visible.
        /// </summary>
        public bool CursorVisible { get; private set; }
        /// <summary>
        /// True if the cursor should be blinking.
        /// </summary>
        public bool CursorBlink { get; private set; }

        private int topVisibleLine;
        private int bottomVisibleLine;

        private int CurrentLine
        {
            get
            {
                return this.topVisibleLine + this.cursorY;
            }
        }

        /// <summary>
        /// Width of the console in characters.
        /// </summary>
        public short Width { get; private set; }
        /// <summary>
        /// Height of the console in characters.
        /// </summary>
        public short Height { get; private set; }

        /// <summary>
        /// Returns the total number of lines in the buffer.
        /// </summary>
        public int BufferSize { get { return this.lines.Size; } }

        public string Title { get; private set; } = string.Empty;

        public Buffer(short width, short height)
        {
            this.Width = width;
            this.Height = height;
            this.CursorVisible = this.CursorBlink = true;
            this.cursorX = this.cursorY = 0;

            this.HandleSGR(new Commands.SetGraphicsRendition(string.Empty));

            for (var y = 0; y < this.Height; ++y)
            {
                this.lines.PushBack(new Line());
            }
            this.topVisibleLine = 0;
            this.bottomVisibleLine = this.MaxCursorY;
        }

        public void SetDimensions(int width, int height)
        {
            if (width < MinimumWidth || width > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            if (height < MinimumHeight || height > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            lock (this.renderLock)
            {
                var heightDiff = height - this.Height;
                if (heightDiff != 0)
                {
                    Logger.Verbose("poop");
                }
                this.Width = (short)width;
                this.Height = (short)height;
                this.topVisibleLine -= heightDiff;
                this.OnPropertyChanged(nameof(this.ViewDimensions));
            }
        }

        /// <summary>
        /// Append the given string to the buffer. The string will be converted to UTF8 and then written to the buffer as usual.
        /// </summary>
        public void AppendString(string str)
        {
            var strBytes = Encoding.UTF8.GetBytes(str);
            this.Append(strBytes, strBytes.Length);
        }

        public void Append(byte[] bytes, int length)
        {
            lock (this.renderLock)
            {
                for (var i = 0;i < length; ++i)
                {
                    if (!this.AppendChar(bytes[i])) continue;

                    ++this.receivedCharacters;
                    switch (this.parser.Append(this.currentChar))
                    {
                    case ParserAppendResult.Render:
                        this.PrintAtCursor(this.currentChar);
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

            if (length > 0)
            {
                this.OnPropertyChanged(string.Empty);
            }
        }

        /// <summary>
        /// Renders the current character at the cursor, advances the cursor, and proceeds to the next line if necessary while scrolling the buffer.
        /// </summary>
        /// <param name="ch"></param>
        private void PrintAtCursor(int ch)
        {
            if (this.cursorX == this.MaxCursorX && this.wrapCharacter == this.receivedCharacters - 1)
            {
                if (this.cursorY == this.MaxCursorY)
                {
                    this.ScrollDown();
                }
                else
                {
                    ++this.cursorY;
                }
                this.cursorX = 0;
                this.wrapCharacter = -1;
            }

            var newChar = this.characterTemplate;
            newChar.Glyph = ch;
            this.lines[this.CurrentLine][this.cursorX] = newChar;

            if (this.cursorX == this.MaxCursorX)
            {
                // if we hit the end of line and our next character is also printable or a backspace we will do an implicit line-wrap.
                // in the appropriate direction. this seems to conform with the expected behavior of applications which emit a variety
                // of control characters/etc to ensure this wiggly state is resolved appropriately.
                this.wrapCharacter = this.receivedCharacters;
            }
            else
            {
                ++this.cursorX;
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
            case Commands.ControlCharacter ctrl:
                this.HandleControlCharacter(ctrl.Code);
                break;
            case Commands.OS osCommand:
                if (osCommand.Command == Commands.OS.Type.SetTitle)
                {
                    this.Title = osCommand.Title;
                    this.OnPropertyChanged("Title");
                }
                break;
            case Commands.ControlSequence csiCommand:
                this.HandleControlSequence(csiCommand);
                break;
            case Commands.Unsupported unsupported:
                break;
            default:
                throw new InvalidOperationException($"Unknown command type passed: {this.parser.Command?.GetType()}.");
            }
        }

        private void HandleControlCharacter(Commands.ControlCharacter.ControlCode code)
        {
            switch (code)
            {
            case Commands.ControlCharacter.ControlCode.NUL:
                // XXX: do we want to print these in some magic way? it seems like most terminals just discard these characters when received.
                break;
            case Commands.ControlCharacter.ControlCode.BEL:
                // XXX: need to raise a beep event.
                break;
            case Commands.ControlCharacter.ControlCode.BS:
                if (this.cursorX == this.MaxCursorX && this.wrapCharacter == this.receivedCharacters - 1)
                {
                    // do nothing, terminal expected us to wrap.
                }
                else
                {
                    this.cursorX = (short)Math.Max(0, this.cursorX - 1);
                }
                break;
            case Commands.ControlCharacter.ControlCode.CR:
                this.cursorX = 0;
                break;
            case Commands.ControlCharacter.ControlCode.FF: // NB: could clear screen with this if we were so inclined. apparently xterm treats this as LF though, let's emulate.
            case Commands.ControlCharacter.ControlCode.LF:
                if (this.CurrentLine == this.bottomVisibleLine)
                {
                    this.ScrollDown();
                }

                this.cursorY = (short)Math.Min(this.MaxCursorY, this.cursorY + 1);
                break;
            case Commands.ControlCharacter.ControlCode.TAB:
                // XXX: we don't handle commands to set tab stops yet but I guess need to do so at some point!
                this.cursorX = (short)Math.Max(this.MaxCursorX, (this.cursorX + 8 - (this.cursorX % 8)));
                break;
            default:
                // XXX: should log the sequence.
                Logger.Verbose("Encountered unsupported sequence.");
                break;
            }
        }

        private void HandleControlSequence(Commands.ControlSequence cmd)
        {
            switch (cmd)
            {
            case Commands.CursorMove cu:
                this.HandleCursorMove(cu);
                break;
            case Commands.EraseCharacter ech:
                this.HandleEraseCharacter(ech);
                break;
            case Commands.EraseIn eid when eid.Type == Commands.EraseIn.EraseType.Display:
                this.HandleEraseInDisplay(eid);
                break;
            case Commands.EraseIn eil when eil.Type == Commands.EraseIn.EraseType.Line:
                this.HandleEraseInLine(eil);
                break;
            case Commands.SetCursorPosition scp:
                this.HandleSetCursorPosition(scp);
                break;
            case Commands.SetGraphicsRendition sgr:
                this.HandleSGR(sgr);
                break;
            case Commands.SetMode sm:
                this.HandleSetMode(sm);
                break;
            default:
                throw new InvalidOperationException($"Unknown CSI command type {cmd.GetType()}.");
            }
        }

        private void HandleCursorMove(Commands.CursorMove cu)
        {
            switch (cu.Direction)
            {
            case Commands.CursorMove.CursorDirection.Up:
                this.cursorY = (short)Math.Max(0, this.cursorY - cu.Count);
                break;
            case Commands.CursorMove.CursorDirection.Down:
                this.cursorY = (short)Math.Min(this.MaxCursorY, this.cursorY + cu.Count);
                break;
            case Commands.CursorMove.CursorDirection.Backward:
                this.cursorX = (short)Math.Max(0, this.cursorX - cu.Count);
                break;
            case Commands.CursorMove.CursorDirection.Forward:
                this.cursorX = (short)Math.Min(this.MaxCursorX, this.cursorX + cu.Count);
                break;
            }
        }

        private void HandleEraseCharacter(Commands.EraseCharacter ech)
        {
            // erase characters starting at the current cursor position and possibly advancing down lines. do not erase below the bottom visible line.
            var y = this.CurrentLine;
            var x = this.cursorX;
            for (var c = 0; c < ech.Count; ++c)
            {
                var eraseChar = this.characterTemplate;
                eraseChar.Glyph = 0x0;
                this.lines[y][x] = eraseChar;
                ++x;
                if (x == this.Width)
                {
                    if (++y > this.bottomVisibleLine)
                    {
                        break;
                    }
                    x = 0;
                }
            }
        }

        private void HandleEraseInDisplay(Commands.EraseIn eid)
        {
            int startY, endY;
            switch (eid.Direction)
            {
            case Commands.EraseIn.Parameter.All:
                startY = this.topVisibleLine;
                endY = this.bottomVisibleLine;
                break;
            case Commands.EraseIn.Parameter.Before:
                startY = this.topVisibleLine;
                endY = this.CurrentLine;
                break;
            case Commands.EraseIn.Parameter.After:
                startY = this.CurrentLine;
                endY = this.bottomVisibleLine;
                break;
            default:
                return;
            }

            var eraseChar = this.characterTemplate;
            eraseChar.Glyph = 0x0;
            for (var y = startY; y <= endY; ++y)
            {
                for (var x = 0; x < this.lines[y].Length; ++x)
                {
                    this.lines[y][x] = eraseChar;
                }
            }
        }

        private void HandleEraseInLine(Commands.EraseIn eil)
        {
            int startX, endX;
            switch (eil.Direction)
            {
            case Commands.EraseIn.Parameter.All:
                startX = 0;
                endX = this.MaxCursorX;
                break;
            case Commands.EraseIn.Parameter.Before:
                startX = 0;
                endX = this.cursorX;
                break;
            case Commands.EraseIn.Parameter.After:
                startX = this.cursorX;
                endX = this.MaxCursorX;
                break;
            default:
                return;
            }

            var eraseChar = this.characterTemplate;
            eraseChar.Glyph = 0x0;
            for (var x = startX; x <= endX; ++x)
            {
                this.lines[this.CurrentLine][x] = eraseChar;
            }
        }

        private void HandleSetCursorPosition(Commands.SetCursorPosition scp)
        {
            if (scp.PosX > -1)
            {
                this.cursorX = (short)Math.Min(this.MaxCursorX, scp.PosX);
            }
            if (scp.PosY > -1)
            {
                this.cursorY = (short)Math.Min(this.MaxCursorY, scp.PosY);
            }
        }

        private void HandleSGR(Commands.SetGraphicsRendition sgr)
        {
            var newTemplate = this.characterTemplate;

            if (sgr.ForegroundBright == Commands.SetGraphicsRendition.FlagValue.Set) newTemplate.Options |= Character.ForegroundBrightFlag;
            if (sgr.ForegroundBright == Commands.SetGraphicsRendition.FlagValue.Unset) newTemplate.Options &= ~Character.ForegroundBrightFlag;
            if (sgr.BackgroundBright == Commands.SetGraphicsRendition.FlagValue.Set) newTemplate.Options |= Character.BackgroundBrightFlag;
            if (sgr.BackgroundBright == Commands.SetGraphicsRendition.FlagValue.Unset) newTemplate.Options &= ~Character.BackgroundBrightFlag;
            if (sgr.Underline == Commands.SetGraphicsRendition.FlagValue.Set) newTemplate.Options |= Character.UnderlineFlag;
            if (sgr.Underline == Commands.SetGraphicsRendition.FlagValue.Unset) newTemplate.Options &= ~Character.UnderlineFlag;
            if (sgr.Inverse == Commands.SetGraphicsRendition.FlagValue.Set) newTemplate.Options |= Character.InverseFlag;
            if (sgr.Inverse == Commands.SetGraphicsRendition.FlagValue.Unset) newTemplate.Options &= ~Character.InverseFlag;

            if (sgr.HaveBasicForeground)
            {
                newTemplate.Options &= ~Character.ForegroundColorFlags;
                newTemplate.Options |= (short)(Character.ForegroundBasicColorFlag | Character.GetColorFlags(sgr.BasicForegroundColor, false));
            }
            else if (sgr.HaveXtermForeground)
            {
                newTemplate.Options &= ~Character.ForegroundColorFlags;
                newTemplate.Options |= Character.ForegroundXterm256Flag;
                newTemplate.Foreground = new Character.ColorInfo { R = (byte)sgr.XtermForegroundColor };
            }
            else if (sgr.HaveForeground)
            {
                newTemplate.Options &= ~(Character.ForegroundBasicColorFlag | Character.ForegroundColorMask);
                newTemplate.Options |= Character.ForegroundRGBFlag;
                newTemplate.Foreground = sgr.ForegroundColor;
            }

            if (sgr.HaveBasicBackground)
            {
                newTemplate.Options &= ~Character.BackgroundColorFlags;
                newTemplate.Options |= (short)(Character.BackgroundBasicColorFlag | Character.GetColorFlags(sgr.BasicBackgroundColor, true));
            }
            else if (sgr.HaveXtermBackground)
            {
                newTemplate.Options &= ~Character.BackgroundColorFlags;
                newTemplate.Options |= Character.BackgroundXterm256Flag;
                newTemplate.Background = new Character.ColorInfo { R = (byte)sgr.XtermBackgroundColor };
            }
            else if (sgr.HaveBackground)
            {
                newTemplate.Options &= ~(Character.BackgroundBasicColorFlag | Character.BackgroundColorMask);
                newTemplate.Options |= Character.BackgroundRGBFlag;
                newTemplate.Background = sgr.BackgroundColor;
            }

            this.characterTemplate = newTemplate;
        }

        private void HandleSetMode(Commands.SetMode sm)
        {
            switch (sm.Setting)
            {
            case Commands.SetMode.Parameter.CursorBlink:
                this.CursorBlink = sm.Set;
                break;
            case Commands.SetMode.Parameter.CursorShow:
                this.CursorVisible = sm.Set;
                break;
            }
        }

        /// <summary>
        /// Scroll the visible buffer down, adding new lines if needed.
        /// If we're at the bottom of the buffer we will replace lines from the top with new, blank lines.
        /// </summary>
        private void ScrollDown(int lines = 1)
        {
            while (lines > 0)
            {
                --lines;
                if (this.bottomVisibleLine == this.lines.Capacity - 1)
                {
                    this.lines.PushBack(new Line()); // will force an old line from the buffer;
                }
                else
                {
                    ++this.topVisibleLine;
                    ++this.bottomVisibleLine;
                    if (this.lines.Size <= this.bottomVisibleLine)
                    {
                        this.lines.PushBack(new Line());
                    }
                }
            }
        }

        /// <summary>
        /// Render the currently "on-screen" area character-by-character onto the specified target.
        /// </summary>
        /// <param name="target">target to render on to.</param>
        public void Render(IRenderTarget target)
        {
            this.RenderFromLine(target, int.MaxValue);
        }

        /// <summary>
        /// Render the specified buffer area character-by-character onto the specified target.
        /// </summary>
        /// <param name="target">Target to render on to.</param>
        /// <param name="startLine">Starting line to render from.</param>
        /// <remarks>
        /// If the starting line would not produce a full render it is silently set to the current top visible line,
        /// producing a render of the current visible screen-buffer. Similarly negative line numbers are treated as 0.
        /// </remarks>
        public void RenderFromLine(IRenderTarget target, int startLine)
        {
            lock (this.renderLock)
            {
                startLine = Math.Max(0, startLine);
                if (startLine > this.topVisibleLine)
                {
                    startLine = this.topVisibleLine;
                }

                for (var y = 0; y < this.Height; ++y)
                {
                    var renderLine = startLine + y;
                    var line = this.lines[renderLine];

                    for (var x = 0; x < this.Width; ++x)
                    {
                        target.RenderCharacter(line[x], x, y);
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
