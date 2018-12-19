namespace condo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;
    using ConsoleBuffer;

    public sealed class Screen : FrameworkElement, IRenderTarget, IScrollInfo
    {
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

        public XtermPalette Palette { get; set; } = XtermPalette.Default;
        public double CellWidth { get; private set; }
        public double CellHeight { get; private set; }

        private bool RenderCursor => this.Buffer.CursorVisible && this.VerticalOffset == this.ExtentHeight - this.ViewportHeight;

        private readonly VisualCollection children;
        private readonly DpiScale dpiInfo;
        private Typeface typeface;
        private GlyphTypeface glyphTypeface;
        private int fontSizeEm = 16;
        private Point baselineOrigin;
        private Rect cellRectangle;
        private double underlineY;
        private double underlineHeight;
        private GuidelineSet cellGuidelines;

        private int horizontalCells, verticalCells;
        private Character[,] characters;

        private bool selecting;
        private Point selectionStart;
        private Point selectionEnd;
        private StringBuilder selectedString = new StringBuilder();

        private volatile int shouldRedraw;
        private int consoleBufferSize;
        private readonly SolidBrushCache brushCache = new SolidBrushCache();

        private static readonly TimeSpan BlinkFrequency = TimeSpan.FromMilliseconds(250);
        private bool cursorInverted;
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
                    this.Buffer.AppendString("\x1b[7m");
                    break;
                case 3:
                    this.Buffer.AppendString("\x1b[4m");
                    break;
                }
                this.Buffer.AppendString($"line {i}\x1b[m\r\n");
            }
#endif
        }

        public Screen(ConsoleBuffer.Buffer buffer)
        {
            this.dpiInfo = VisualTreeHelper.GetDpi(this);
            this.children = new VisualCollection(this) { new DrawingVisual { Offset = new Vector(0, 0) } };
            this.Buffer = buffer;

            this.cursorBlinkWatch.Start();

            CompositionTarget.Rendering += this.RenderFrame;
            this.MouseEnter += (sender, args) =>
            {
                args.MouseDevice.OverrideCursor = Cursors.IBeam;
                
                // handles the case where the user released the mouse button outside the window.
                if (this.selecting && args.LeftButton == MouseButtonState.Released)
                {
                    this.CompleteSelection();
                }
            };
            this.MouseLeave += (sender, args) =>
            {
                args.MouseDevice.OverrideCursor = Cursors.Arrow;
            };
            this.MouseWheel += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    var factor = args.Delta > 0 ? 2 : -2;
                    this.SetFontSize(this.fontSizeEm + factor);
                    args.Handled = true;
                }
            };

            this.MouseLeftButtonDown += (sender, args) =>
            {
                this.selecting = true;
                this.selectionStart = this.selectionEnd = args.GetPosition(this);
                args.Handled = true;
            };
            this.MouseLeftButtonUp += (sender, args) =>
            {
                this.selectionEnd = args.GetPosition(this);
                Logger.Verbose($"sel: start:{this.selectionStart}, end:{this.selectionEnd}");
                this.selectionStart = this.selectionEnd = new Point(0, 0);
                this.CompleteSelection();
                args.Handled = true;
            };
            this.MouseMove += (sender, args) =>
            {
                if (this.selecting)
                {
                    this.selectionEnd = args.GetPosition(this);
                    this.shouldRedraw = 1;
                }
            };

            this.SetFontSize(14);
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
            else if (args.PropertyName == nameof(this.Buffer.ViewDimensions))
            {
                this.Resize();
            }
        }

        private void RenderFrame(object sender, EventArgs e)
        {
            if (this.RenderCursor && this.cursorBlinkWatch.Elapsed >= BlinkFrequency)
            {
                this.cursorBlinkWatch.Restart();
                // XXX: minor, but we should really just not redraw every BlinkFrequency intervals if the cursor isn't supposed to be blinking.
                this.cursorInverted = this.Buffer.CursorBlink ? !this.cursorInverted : true;
                this.shouldRedraw = 1;
            }

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
        }

        public void Close()
        {
            this.Buffer.PropertyChanged -= this.OnBufferPropertyChanged;
        }

        protected override int VisualChildrenCount => this.children.Count;

        protected override Visual GetVisualChild(int index)
        {
            return this.children[index];
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.ActualHeight > availableSize.Height || this.ActualWidth > availableSize.Width)
            {
                var x = Math.Max(ConsoleBuffer.Buffer.MinimumWidth, (int)Math.Floor(availableSize.Width / this.CellWidth));
                var y = Math.Max(ConsoleBuffer.Buffer.MinimumHeight, (int)Math.Floor(availableSize.Height / this.CellHeight));
                return new Size(this.CellWidth * x, this.CellHeight * y);
            }

            return new Size(this.horizontalCells * this.CellWidth, this.verticalCells * this.CellHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = (int)Math.Floor(finalSize.Width / this.CellWidth);
            var y = (int)Math.Floor(finalSize.Height / this.CellHeight);
            this.Buffer.SetDimensions(x, y);
            return new Size(this.CellWidth * x, this.CellHeight * y);
        }

        private void Resize()
        {
            this.horizontalCells = this.Buffer.Width;
            this.verticalCells = this.Buffer.Height;
            this.characters = new Character[this.Buffer.Width, this.Buffer.Height];

            this.cellGuidelines = new GuidelineSet();
            this.cellGuidelines.GuidelinesX.Add(0);
            this.cellGuidelines.GuidelinesY.Add(0);
            for (var x = 0; x < this.horizontalCells; ++x)
            {
                this.cellGuidelines.GuidelinesX.Add(this.CellWidth * (x + 1));
            }
            for (var y = 0; y < this.verticalCells; ++y)
            {
                this.cellGuidelines.GuidelinesY.Add(this.CellHeight * (y + 1));
            }

            this.consoleBufferSize = this.Buffer.BufferSize;
            this.cellGuidelines.Freeze();

            this.shouldRedraw = 1;
        }

        private void SetFontSize(int newFontSizeEm)
        {
            newFontSizeEm = Math.Max(8, Math.Min(72, newFontSizeEm));

            if (this.fontSizeEm == newFontSizeEm)
            {
                return;
            }

            this.fontSizeEm = newFontSizeEm;

            this.typeface = new Typeface("Consolas"); // TODO: do something if we can't find this.
            if (!this.typeface.TryGetGlyphTypeface(out this.glyphTypeface))
            {
                throw new InvalidOperationException("Could not get desired font.");
            }
            this.CellWidth = this.glyphTypeface.AdvanceWidths[0] * this.fontSizeEm;
            this.CellHeight = this.glyphTypeface.Height * this.fontSizeEm;
            this.baselineOrigin = new Point(0, this.glyphTypeface.Baseline * this.fontSizeEm);
            this.underlineY = this.baselineOrigin.Y - this.glyphTypeface.UnderlinePosition * this.fontSizeEm;
            this.underlineHeight = (this.CellHeight * this.glyphTypeface.UnderlineThickness);
            this.cellRectangle = new Rect(new Size(this.CellWidth, this.CellHeight));

            this.Resize();
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

        private void Redraw()
        {
            var dv = this.children[0] as DrawingVisual;

            using (var dc = dv.RenderOpen())
            {
                dc.PushGuidelineSet(this.cellGuidelines);

                this.RedrawText(dc);
                this.RedrawSelection(dc);

                dc.Pop();
            }
        }

        private void RedrawSelection(DrawingContext dc)
        {
            // if the user is selecting from the bottom up treat that as our end point instead.
            var startLine = this.selectionStart;
            var endLine = this.selectionEnd;
            if (startLine.Y > endLine.Y)
            {
                var swap = startLine;
                startLine = endLine;
                endLine = swap;
            }

            var left = startLine.X;
            var right = endLine.X;
            var top = startLine.Y;
            var bottom = endLine.Y;

            if (!this.selecting || (left == right && top == bottom))
            {
                return;
            }

            // walk every line, skipping those outside the range of the selection. For every selected character append it to
            // our selected string data raw (this will be cleaned later) and draw appropriate per-line rectangles
            this.selectedString.Clear();
            for (var y = 0; y < this.verticalCells; ++y)
            {
                var lineTop = y * this.CellHeight;
                var lineBottom = lineTop + this.CellHeight;
                var drawLeft = 0.0;
                var drawWidth = this.horizontalCells * this.CellWidth;

                if (lineBottom < top || lineTop > bottom) continue; // line is outside the selection range.

                if (lineTop <= top)
                {
                    drawLeft = startLine.X;
                }

                if (lineBottom >= bottom)
                {
                    drawWidth = right;
                    if (drawLeft > 0.0)
                    {
                        // single line selects mean we might need to swap left/right actually.
                        left = Math.Min(startLine.X, endLine.X);
                        right = Math.Max(startLine.X, endLine.X);
                        drawLeft = left;
                        drawWidth = right - left;
                    }
                    drawWidth += drawWidth % this.CellWidth;
                }

                if (this.selectedString.Length > 0) this.selectedString.Append('\n');
                for (var x = 0; x < this.horizontalCells; ++x)
                {
                    if (x * this.CellWidth >= drawLeft && (x + 1) * this.CellWidth <= drawLeft + drawWidth)
                    {
                        this.selectedString.Append((char)this.characters[x, y].Glyph);
                    }
                }

                dc.DrawRectangle(new SolidColorBrush(new Color { R = 127, G = 127, B = 255, A = 95 }), null, new Rect(drawLeft, lineTop, drawWidth, this.CellHeight));
            }
        }

        private void RedrawText(DrawingContext dc)
        {
            var glyphChars = new List<ushort>(this.horizontalCells);
            var advanceWidths = new List<double>(this.horizontalCells);
            // Render line by line, attempting to render as long as properties remain the same or we reach the end of the line.
            for (var y = 0; y < this.verticalCells; ++y)
            {
                var runStart = 0;
                var x = 0;
                var allNull = true;
                var allEmpty = true;

                while (x < this.horizontalCells)
                {
                    var currentGlyph = this.characters[x, y].Glyph;
                    allNull &= currentGlyph == 0x0;
                    allEmpty &= (currentGlyph == 0x0 || currentGlyph == 0x20);
                    ++x;

                    // Properties that can change and cause a run stop + render:
                    // - Any of the basic character properties (bright/inverse/colors/etc)
                    // - Hitting a visible cursor (as we may need to invert it explicitly -- we have to stop both at the cursor and immediately before it.)
                    // - Hitting a "null" character (this may be a terminator for certain properties such as underlining/inverse/etc)
                    if (x == this.horizontalCells
                        || !this.characters[runStart, y].PropertiesEqual(this.characters[x, y])
                        || (this.RenderCursor && ((runStart, y) == this.Buffer.CursorPosition) || (x, y) == this.Buffer.CursorPosition)
                        || (this.characters[x, y].Glyph == 0x0 && this.characters[runStart, y].Glyph != 0x0))
                    {
                        var charCount = x - runStart;
                        var startChar = this.characters[runStart, y];
                        var invert = startChar.Inverse && startChar.Glyph != 0x0;

                        // this isn't super intuitive from the above but we'll stop a run if the cursor is visible so we can render it specially,
                        // here that means potentially inverting it depending on our own 'blink' state.
                        if (this.RenderCursor && charCount == 1 && (runStart, y) == this.Buffer.CursorPosition && this.cursorInverted)
                        {
                            invert = !invert;
                        }

                        Character.ColorInfo fg, bg;
                        if (!invert) (fg, bg) = this.GetCharacterColors(startChar);
                        else (bg, fg) = this.GetCharacterColors(startChar);

                        var backgroundBrush = this.brushCache.GetBrush(bg.R, bg.G, bg.B);
                        var foregroundBrush = this.brushCache.GetBrush(fg.R, fg.G, fg.B);

                        dc.DrawRectangle(backgroundBrush, null, new Rect(runStart * this.CellWidth, y * this.CellHeight, charCount * this.CellWidth, this.CellHeight));

                        // if all characters are null and we're at EOL stop after rendering only the background
                        // XXX: this feels REALLY hacky to me
                        if (x == this.horizontalCells && allNull)
                        {
                            continue;
                        }

                        var glyphOrigin = new Point((runStart * this.CellWidth) + this.baselineOrigin.X, (y * this.CellHeight) + this.baselineOrigin.Y);
                        if (startChar.Underline)
                        {
                            var underlineRectangle = new Rect(glyphOrigin.X, y * this.CellHeight + this.underlineY, charCount * this.CellWidth, this.underlineHeight);
                            dc.DrawRectangle(foregroundBrush, null, underlineRectangle);
                        }

                        glyphChars.Clear();
                        advanceWidths.Clear();
                        for (var c = runStart; c < x; ++c)
                        {
                            if (!this.glyphTypeface.CharacterToGlyphMap.TryGetValue((char)this.characters[c, y].Glyph, out var glyphIndex))
                            {
                                glyphIndex = 0;
                            }
                            glyphChars.Add(glyphIndex);
                            advanceWidths.Add(this.glyphTypeface.AdvanceWidths[glyphIndex] * this.fontSizeEm);
                        }

                        if (!allEmpty)
                        {
                            var gr = new GlyphRun(this.glyphTypeface, 0, false, this.fontSizeEm, (float)this.dpiInfo.PixelsPerDip, new List<ushort>(glyphChars),
                                glyphOrigin, new List<double>(advanceWidths), null, null, null, null, null, null);

                            dc.DrawGlyphRun(foregroundBrush, gr);
                        }

                        runStart = x;
                        allNull = allEmpty = true;
                    }
                }
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }

        private static readonly char[] PasteTrimChars = new[] { '\0', ' ', '\n' };
        private void CompleteSelection()
        {
            var pasteText = new StringBuilder(this.selectedString.Length);
            this.selecting = false;
            // NB: this code could be more efficient but should not be frequently hit
            foreach (var line in this.selectedString.ToString().Split('\n'))
            {
                if (pasteText.Length > 0) pasteText.Append('\n');
                pasteText.Append(line.TrimEnd(PasteTrimChars));
            }

            Clipboard.SetText(pasteText.ToString());
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
