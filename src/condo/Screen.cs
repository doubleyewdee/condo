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
                this.buffer.Palette = this.palette;
            }
        }

        private readonly XtermPalette palette = XtermPalette.Default;

        private VisualCollection cells;
        private DpiScale dpiInfo;
        private readonly GlyphTypeface typeface;
        private readonly int fontSize = 16;
        private readonly double cellWidth, cellHeight;
        private readonly Point baselineOrigin;
        private readonly Rect cellRectangle;
        private int horizontalCells, verticalCells;
        private Character[,] characters;
        bool cursorInverted;
        private volatile int shouldRedraw;
        private int consoleBufferSize;

        private static readonly TimeSpan MaxRedrawFrequency = TimeSpan.FromMilliseconds(10);
        private readonly Stopwatch redrawWatch = new Stopwatch();
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
                var line = System.Text.Encoding.UTF8.GetBytes($"line {i}\r\n");
                this.Buffer.Append(line, line.Length);
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
            this.cellRectangle = new Rect(new Size(this.cellWidth, this.cellHeight));

            this.Buffer = buffer;
            this.redrawWatch.Start();
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
            if (this.redrawWatch.Elapsed >= MaxRedrawFrequency && this.shouldRedraw != 0)
            {
                this.redrawWatch.Restart();
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
            this.characters = new Character[this.Buffer.Width, this.Buffer.Height];

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

        public void SetCellCharacter(int x, int y, bool invert = false)
        {
            var ch = this.characters[x, y];

            using (var dc = this.GetCell(x, y).RenderOpen())
            {
                GlyphRun gr;
                try
                {
                    gr = new GlyphRun(this.typeface, 0, false, this.fontSize, (float)this.dpiInfo.PixelsPerDip, new[] { this.typeface.CharacterToGlyphMap[(char)ch.Glyph] },
                        this.baselineOrigin, new[] { 0.0 }, new[] { new Point(0, 0) }, null, null, null, null, null);
                }
                catch (KeyNotFoundException)
                {
                    gr = new GlyphRun(this.typeface, 0, false, this.fontSize, (float)this.dpiInfo.PixelsPerDip, new[] { this.typeface.CharacterToGlyphMap[0] },
                        this.baselineOrigin, new[] { 0.0 }, new[] { new Point(0, 0) }, null, null, null, null, null);
                }

                var backgroundBrush = new SolidColorBrush(new Color { R = ch.Background.R, G = ch.Background.G, B = ch.Background.B, A = 255 });
                var foregroundBrush = new SolidColorBrush(new Color { R = ch.Foreground.R, G = ch.Foreground.G, B = ch.Foreground.B, A = 255 });
                dc.DrawRectangle(!invert ? backgroundBrush : foregroundBrush, null, new Rect(new Point(0, 0), new Point(this.cellWidth, this.cellHeight)));
                dc.DrawGlyphRun(!invert ? foregroundBrush : backgroundBrush, gr);
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }

        private void Redraw()
        {
            for (var x = 0; x < this.Buffer.Width; ++x)
            {
                for (var y = 0; y < this.Buffer.Height; ++y)
                {
                    this.SetCellCharacter(x, y);
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
