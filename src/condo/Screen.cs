namespace condo
{
    using System;
    using System.Collections.Generic;
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
            }
        }

        private readonly VisualCollection children;
        private readonly DpiScale dpiInfo;
        private Typeface typeface;
        private GlyphTypeface glyphTypeface;
        private int fontSizeEm = 16;
        private double cellWidth, cellHeight;
        private Point baselineOrigin;
        private Rect cellRectangle;
        private double underlineY;
        private double underlineHeight;
        private GuidelineSet cellGuidelines;
        private int horizontalCells, verticalCells;
        private Character[,] characters;
        bool cursorInverted;
        private volatile int shouldRedraw;
        private int consoleBufferSize;
        private readonly SolidBrushCache brushCache = new SolidBrushCache();

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
            this.children = new VisualCollection(this);
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
            this.MouseWheel += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    var factor = args.Delta > 0 ? 2 : -2;
                    this.SetFontSize(this.fontSizeEm + factor);
                    args.Handled = true;
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

            /*
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
            */
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
            return new Size(this.cellWidth * this.horizontalCells, this.cellHeight * this.verticalCells);
        }

        private void Resize()
        {
            this.horizontalCells = this.Buffer.Width;
            this.verticalCells = this.Buffer.Height;
            this.characters = new Character[this.Buffer.Width, this.Buffer.Height];

            this.children.Clear();
            this.children.Add(new DrawingVisual { Offset = new Vector(0, 0) });

            this.cellGuidelines = new GuidelineSet();
            this.cellGuidelines.GuidelinesX.Add(0);
            this.cellGuidelines.GuidelinesY.Add(0);
            for (var x = 0; x < this.horizontalCells; ++x)
            {
                this.cellGuidelines.GuidelinesX.Add(this.cellWidth * (x + 1));
            }
            for (var y = 0; y < this.verticalCells; ++y)
            {
                this.cellGuidelines.GuidelinesY.Add(this.cellHeight * (y + 1));
            }

            this.Width = this.horizontalCells * this.cellWidth;
            this.Height = this.verticalCells * this.cellHeight;
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
            this.cellWidth = this.glyphTypeface.AdvanceWidths[0] * this.fontSizeEm;
            this.cellHeight = this.glyphTypeface.Height * this.fontSizeEm;
            this.baselineOrigin = new Point(0, this.glyphTypeface.Baseline * this.fontSizeEm);
            this.underlineY = this.baselineOrigin.Y - this.glyphTypeface.UnderlinePosition * this.fontSizeEm;
            this.underlineHeight = (this.cellHeight * this.glyphTypeface.UnderlineThickness);
            this.cellRectangle = new Rect(new Size(this.cellWidth, this.cellHeight));

            this.Resize();
        }

        private DrawingVisual GetCell(int x, int y)
        {
            return this.children[x + y * this.horizontalCells] as DrawingVisual;
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
            var glyphChars = new List<ushort>(this.horizontalCells);
            var advanceWidths = new List<double>(this.horizontalCells);

            using (var dc = dv.RenderOpen())
            {
                dc.PushGuidelineSet(this.cellGuidelines);

                // Render line by line, attempting to render as long as properties remain the same or we reach the end of the line.
                // Properties that can change and cause a render stop:
                // - Any of the basic character properties (bright/inverse/colors/etc)
                // - Hitting a "null" character (this is a terminator for certain properties such as underlining/inverse/etc)
                // - Hitting a visible cursor (as we may need to invert it explicitly)
                for (var y = 0; y < this.verticalCells; ++y)
                {
                    var runStart = 0;
                    var x = 0;
                    
                    while (x < this.horizontalCells)
                    {
                        ++x;

                        if (   x == this.horizontalCells
                            || !this.characters[runStart, y].PropertiesEqual(this.characters[x, y])
                            || (this.characters[x, y].Glyph == 0x0 && this.characters[runStart, y].Glyph != 0x0)
                            || (this.Buffer.CursorVisible && (x, y) == this.Buffer.CursorPosition))
                        {
                            var startX = runStart;
                            var charCount = x - startX;
                            runStart = x;
                            var startChar = this.characters[startX, y];
                            var invert = startChar.Inverse && startChar.Glyph != 0x0;

                            Character.ColorInfo fg, bg;
                            if (!invert) (fg, bg) = this.GetCharacterColors(startChar);
                            else (bg, fg) = this.GetCharacterColors(startChar);

                            var backgroundBrush = this.brushCache.GetBrush(bg.R, bg.G, bg.B);
                            var foregroundBrush = this.brushCache.GetBrush(fg.R, fg.G, fg.B);

                            dc.DrawRectangle(backgroundBrush, null, new Rect(startX * this.cellWidth, y * this.cellHeight, charCount * this.cellWidth, this.cellHeight));

                            // If we began with a null character it should be safe to stop with rendering the background.
                            if (fg == bg || startChar.Glyph == 0x0)
                            {
                                continue;
                            }

                            var glyphOrigin = new Point((startX * this.cellWidth) + this.baselineOrigin.X, (y * this.cellHeight) + this.baselineOrigin.Y);
                            if (startChar.Underline)
                            {
                                var underlineRectangle = new Rect(glyphOrigin.X, y * this.cellHeight + this.underlineY, charCount * this.cellWidth, this.underlineHeight);
                                dc.DrawRectangle(foregroundBrush, null, underlineRectangle);
                            }

                            var content = false;
                            glyphChars.Clear();
                            advanceWidths.Clear();
                            for (var c = startX; c < x; ++c)
                            {
                                content |= this.characters[c, y].Glyph != 0x20;

                                if (!this.glyphTypeface.CharacterToGlyphMap.TryGetValue((char)this.characters[c, y].Glyph, out var glyphIndex))
                                {
                                    glyphIndex = 0;
                                }
                                glyphChars.Add(glyphIndex);
                                advanceWidths.Add(this.glyphTypeface.AdvanceWidths[glyphIndex] * this.fontSizeEm);
                            }

                            if (content)
                            {
                                var gr = new GlyphRun(this.glyphTypeface, 0, false, this.fontSizeEm, (float)this.dpiInfo.PixelsPerDip, new List<ushort>(glyphChars),
                                    glyphOrigin, new List<double>(advanceWidths), null, null, null, null, null, null);

                                dc.DrawGlyphRun(foregroundBrush, gr);
                            }
                        }
                    }
                }

                dc.Pop();
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            this.characters[x, y] = c;
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
