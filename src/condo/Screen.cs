namespace condo
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using ConsoleBuffer;

    public sealed class Screen : FrameworkElement, IRenderTarget, IScrollInfo
    {
        private struct DrawCharacter
        {
            public bool Changed;
            public Character Character;
        }

        private ConsoleBuffer.Buffer buffer;
        public ConsoleBuffer.Buffer Buffer
        {
            get { return this.buffer; }
            set
            {
                if (this.buffer != null)
                {
                    this.Buffer.PropertyChanged -= this.OnBufferPropertyChanged;
                }

                this.buffer = value ?? throw new ArgumentNullException(nameof(value));
                this.Resize();
                this.buffer.PropertyChanged += this.OnBufferPropertyChanged;
            }
        }

        private XtermPalette palette = XtermPalette.Default;
        public XtermPalette Palette
        {
            get
            {
                return this.palette;
            }
            set
            {
                this.palette = value;
                for (var x = 0; x < this.buffer.Width; ++x)
                {
                    for (var y = 0; y < this.buffer.Height; ++y)
                    {
                        this.characters[x, y].Changed = true;
                    }
                }
            }
        }

        private VisualCollection cells;
        private DpiScale dpiInfo;
        private readonly GlyphTypeface typeface;
        private readonly int fontSize = 16;
        private readonly double cellWidth, cellHeight;
        private readonly Point baselineOrigin;
        private readonly Rect cellRectangle;
        private readonly double underlineY;
        private readonly double underlineHeight;
        private readonly GuidelineSet cellGuidelines;
        private int horizontalCells, verticalCells;
        private DrawCharacter[,] characters;
        bool cursorInverted;
        private volatile int shouldRedraw;
        private int consoleBufferSize;
        private SolidBrushCache brushCache = new SolidBrushCache();

        private static readonly TimeSpan BlinkFrequency = TimeSpan.FromMilliseconds(250);
        private readonly Stopwatch cursorBlinkWatch = new Stopwatch();

        /// <summary>
        /// Empty ctor for designer purposes at present. Probably don't use.
        /// </summary>
        public Screen() : this(new ConsoleBuffer.Buffer(80, 25))
        {
#if DEBUG
            for (var i = 0; i < 100; ++i)
            {
                switch (i % 4)
                {
                case 1:
                    this.Buffer.AppendString("\x1b[1m");
                    break;
                case 2:
                    this.Buffer.AppendString("\x1b[4m");
                    break;
                case 3:
                    this.Buffer.AppendString("\x1b[7m");
                    break;
                }
                this.Buffer.AppendString($"line {i}\x1b[m\r\n");
            }
#endif
        }

        public Screen(ConsoleBuffer.Buffer buffer)
        {
            this.dpiInfo = VisualTreeHelper.GetDpi(this);
            this.cells = new VisualCollection(this);
            if (!new Typeface("Consolas").TryGetGlyphTypeface(out this.typeface))
            {
                throw new InvalidOperationException("Could not get desired font.");
            }
            this.cellWidth = this.typeface.AdvanceWidths[0] * this.fontSize;
            this.cellHeight = this.typeface.Height * this.fontSize;
            this.baselineOrigin = new Point(0, this.typeface.Baseline * this.fontSize);
            this.underlineY = this.baselineOrigin.Y - this.typeface.UnderlinePosition * this.fontSize;
            this.underlineHeight = (this.cellHeight * this.typeface.UnderlineThickness);
            this.cellRectangle = new Rect(new Size(this.cellWidth, this.cellHeight));
            this.cellGuidelines = new GuidelineSet(
                new[] { this.cellRectangle.Left, this.cellRectangle.Right },
                new[] { this.cellRectangle.Top, this.underlineY, this.underlineY + this.underlineHeight, this.cellRectangle.Bottom });
            this.cellGuidelines.Freeze();

            this.Buffer = buffer;
            this.cursorBlinkWatch.Start();

            CompositionTarget.Rendering += this.RenderFrame;
            this.MouseEnter += (sender, args) =>
            {
                args.MouseDevice.OverrideCursor = Cursors.IBeam;
            };
            this.MouseLeave += (sender, args) =>
            {
                args.MouseDevice.OverrideCursor = Cursors.Arrow;
            };

            this.Resize();
        }

        private void OnBufferPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == string.Empty)
            {
                // if we're scrolled to the bottom prior to redrawing (the conditional) indicate that we should render,
                // if we are not scrolled to the bottom we don't need to render unless the offset is changed by the
                // user.
                if (this.VerticalOffset == this.ExtentHeight - this.ViewportHeight)
                {
                    this.shouldRedraw = 1;
                }
            }
        }

        private void RenderFrame(object sender, EventArgs e)
        {
            if (this.shouldRedraw != 0)
            {
                this.shouldRedraw = 0;

                // when rendering we should update our view of the buffer size, and if we were previously scrolled
                // to the bottom ensure we stay that way after doing so.
                var bufferSize = this.Buffer.BufferSize;
                var updateOffset = this.VerticalOffset == this.ExtentHeight - this.ViewportHeight;
                this.consoleBufferSize = bufferSize;
                if (updateOffset)
                {
                    this.VerticalOffset = double.MaxValue;
                }

                var startLine = this.VerticalOffset;
                this.Buffer.RenderFromLine(this, (int)startLine);
                this.Redraw();
                this.ScrollOwner?.ScrollToVerticalOffset(this.VerticalOffset);
            }

            if (this.Buffer.CursorVisible && this.VerticalOffset == this.ExtentHeight - this.ViewportHeight)
            {
                if (this.cursorBlinkWatch.Elapsed >= BlinkFrequency)
                {
                    this.cursorBlinkWatch.Restart();

                    this.cursorInverted = this.Buffer.CursorBlink ? !this.cursorInverted : true;
                    (var x, var y) = this.Buffer.CursorPosition;
                    this.SetCellCharacter(x, y, this.cursorInverted);
                }
            }
        }

        public void Close()
        {
            this.Buffer.PropertyChanged -= this.OnBufferPropertyChanged;
        }

        protected override int VisualChildrenCount => this.cells.Count;

        protected override Visual GetVisualChild(int index)
        {
            return this.cells[index];
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(this.cellWidth * this.horizontalCells, this.cellHeight * this.verticalCells);
        }

        private void Resize()
        {
            this.cells.Clear();

            this.horizontalCells = this.Buffer.Width;
            this.verticalCells = this.Buffer.Height;
            this.characters = new DrawCharacter[this.Buffer.Width, this.Buffer.Height];

            for (var y = 0; y < this.verticalCells; ++y)
            {
                for (var x = 0; x < this.horizontalCells; ++x)
                {
                    var dv = new DrawingVisual();
                    dv.Offset = new Vector(x * this.cellWidth, y * this.cellHeight);
                    this.cells.Add(dv);
                }
            }

            this.Width = this.horizontalCells * this.cellWidth;
            this.Height = this.verticalCells * this.cellHeight;
            this.consoleBufferSize = this.Buffer.BufferSize;
        }

        private DrawingVisual GetCell(int x, int y)
        {
            return this.cells[x + y * this.horizontalCells] as DrawingVisual;
        }

        private (Character.ColorInfo fg, Character.ColorInfo bg) GetCharacterColors(Character ch)
        {
            var cfg = ch.Foreground;
            if (ch.BasicForegroundColor != ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None)
            {
                cfg = this.GetColorInfoFromBasicColor(ch.BasicForegroundColor, ch.ForegroundBright);
            }
            else if (ch.ForegroundXterm256)
            {
                cfg = this.Palette[ch.ForegroundXterm256Index];
            }

            var cbg = ch.Background;
            if (ch.BasicBackgroundColor != ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None)
            {
                cbg = this.GetColorInfoFromBasicColor(ch.BasicBackgroundColor, ch.BackgroundBright);
            }
            else if (ch.BackgroundXterm256)
            {
                cfg = this.Palette[ch.BackgroundXterm256Index];
            }

            return (cfg, cbg);
        }

        private Character.ColorInfo GetColorInfoFromBasicColor(ConsoleBuffer.Commands.SetGraphicsRendition.Colors basicColor, bool isBright)
        {
            var paletteOffset = isBright ? 8 : 0;
            switch (basicColor)
            {
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black:
                return this.Palette[0 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red:
                return this.Palette[1 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green:
                return this.Palette[2 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow:
                return this.Palette[3 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue:
                return this.Palette[4 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta:
                return this.Palette[5 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan:
                return this.Palette[6 + paletteOffset];
            case ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White:
                return this.Palette[7 + paletteOffset];
            default:
                throw new InvalidOperationException("Unexpected color value.");
            }
        }

        private void SetCellCharacter(int x, int y, bool invert = false)
        {
            var ch = this.characters[x, y].Character;
            invert ^= ch.Inverse;

            using (var dc = this.GetCell(x, y).RenderOpen())
            {
                var gs = new GuidelineSet();
                dc.PushGuidelineSet(this.cellGuidelines);
                (var fg, var bg) = this.GetCharacterColors(ch);
                var backgroundBrush = this.brushCache.GetBrush(bg.R, bg.G, bg.B);
                var foregroundBrush = this.brushCache.GetBrush(fg.R, fg.G, fg.B);
                dc.DrawRectangle(!invert || ch.Glyph == 0x0 ? backgroundBrush : foregroundBrush, null, this.cellRectangle);

                if (fg == bg || ch.Glyph == 0x0)
                {
                    // if the glyph is unset or we have the same fg/bg we can bail out early (draw a 'blank')
                    // note we can't treat 0x20 as a blank as this breaks underlining.
                    return;
                }

                if (ch.Underline)
                {
                    var underlineRectangle = new Rect(0, this.underlineY, this.cellWidth, this.underlineHeight);
                    dc.DrawRectangle(!invert ? foregroundBrush : backgroundBrush, null, underlineRectangle);
                }

                if (ch.Glyph == 0x20)
                {
                    // okay NOW we can gtfo.
                    return;
                }

                GlyphRun gr;
                if (!this.typeface.CharacterToGlyphMap.TryGetValue((char)ch.Glyph, out var glyphValue))
                {
                    glyphValue = 0;
                }
                gr = new GlyphRun(this.typeface, 0, false, this.fontSize, (float)this.dpiInfo.PixelsPerDip, new[] { glyphValue },
                    this.baselineOrigin, new[] { 0.0 }, new[] { new Point(0, 0) }, null, null, null, null, null);

                dc.DrawGlyphRun(!invert ? foregroundBrush : backgroundBrush, gr);
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            if (c != this.characters[x, y].Character)
            {
                this.characters[x, y].Changed = true;
                this.characters[x, y].Character = c;
            }
        }

        private void Redraw()
        {
            for (var x = 0; x < this.Buffer.Width; ++x)
            {
                for (var y = 0; y < this.Buffer.Height; ++y)
                {
                    this.SetCellCharacter(x, y);
                    this.characters[x, y].Changed = false;
                }
            }
        }

#region IScrollInfo
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth => this.Buffer.Width;

        public double ExtentHeight => this.consoleBufferSize;

        public double ViewportWidth => this.Buffer.Width;

        public double ViewportHeight => this.Buffer.Height;

        public double HorizontalOffset => 0.0;

        private double verticalOffset;
        public double VerticalOffset
        {
            get
            {
                return this.verticalOffset;
            }
            set
            {
                var newValue = Math.Max(0, Math.Min(this.ExtentHeight - this.ViewportHeight, value));
                if (this.verticalOffset != newValue)
                {
                    this.verticalOffset = newValue;
                    this.shouldRedraw = 1;
                }
            }
        }

        public ScrollViewer ScrollOwner { get; set; }

        public void LineUp()
        {
            this.VerticalOffset -= 1;
        }

        public void LineDown()
        {
            this.VerticalOffset += 1;
        }

        public void LineLeft() { }

        public void LineRight() { }

        public void PageUp()
        {
            this.VerticalOffset -= this.Buffer.Height;
        }

        public void PageDown()
        {
            this.VerticalOffset += this.Buffer.Height;
        }

        public void PageLeft() { }

        public void PageRight() { }

        public void MouseWheelUp()
        {
            this.VerticalOffset -= 3;
        }

        public void MouseWheelDown()
        {
            this.VerticalOffset += 3;
        }

        public void MouseWheelLeft() { }

        public void MouseWheelRight() { }

        public void SetHorizontalOffset(double offset) { }

        public void SetVerticalOffset(double offset)
        {
            this.VerticalOffset = offset;
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            throw new NotImplementedException();
        }
#endregion
    }
}
