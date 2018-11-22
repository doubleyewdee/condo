namespace condo
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using ConsoleBuffer;

    public sealed class Screen : FrameworkElement, IRenderTarget
    {
        public ConsoleWrapper Console { get; }

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

        private static readonly TimeSpan MaxRedrawFrequency = TimeSpan.FromMilliseconds(10);
        private readonly Stopwatch redrawWatch = new Stopwatch();
        private static readonly TimeSpan BlinkFrequency = TimeSpan.FromMilliseconds(250);
        private readonly Stopwatch cursorBlinkWatch = new Stopwatch();

        public Screen(ConsoleWrapper console)
        {
            this.dpiInfo = VisualTreeHelper.GetDpi(this);
            this.cells = new VisualCollection(this);
            if (!new Typeface("Consolas").TryGetGlyphTypeface(out this.typeface))
            {
                throw new InvalidOperationException("Could not get desired font.");
            }

            this.Console = console;
            this.horizontalCells = console.Width;
            this.verticalCells = console.Height;
            this.characters = new Character[this.Console.Width, this.Console.Height];

            this.cellWidth = this.typeface.AdvanceWidths[0] * this.fontSize;
            this.cellHeight = this.typeface.Height * this.fontSize;
            this.baselineOrigin = new Point(0, this.typeface.Baseline * this.fontSize);
            this.cellRectangle = new Rect(new Size(this.cellWidth, this.cellHeight));

            this.redrawWatch.Start();
            this.cursorBlinkWatch.Start();

            this.Console.PropertyChanged += this.UpdateContents;
            CompositionTarget.Rendering += this.RenderFrame;
            this.MouseEnter += (sender, args) =>
            {
                args.MouseDevice.OverrideCursor = Cursors.IBeam;
            };

            this.Resize();
        }

        private void RenderFrame(object sender, EventArgs e)
        {
            if (this.redrawWatch.Elapsed >= MaxRedrawFrequency && this.shouldRedraw != 0)
            {
                this.shouldRedraw = 0;
                this.Console.Buffer.Render(this);
                this.Redraw();
                this.redrawWatch.Restart();
            }

            if (this.Console.Buffer.CursorVisible)
            {
                if (this.cursorBlinkWatch.Elapsed >= BlinkFrequency)
                {
                    this.cursorInverted = this.Console.Buffer.CursorBlink ? !this.cursorInverted : true;
                    (var x, var y) = this.Console.Buffer.CursorPosition;
                    this.SetCellCharacter(x, y, (char)this.characters[x, y].Glyph, this.cursorInverted);
                    this.cursorBlinkWatch.Restart();
                }
            }
        }

        public void Close()
        {
            this.Console.PropertyChanged -= this.UpdateContents;
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

        private void UpdateContents(object sender, PropertyChangedEventArgs args)
        {
            this.shouldRedraw = 1;
        }

        private void Resize()
        {
            this.cells.Clear();
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
        }

        private DrawingVisual GetCell(int x, int y)
        {
            return this.cells[x + y * this.horizontalCells] as DrawingVisual;
        }

        public void SetCellCharacter(int x, int y, char c, bool invert = false)
        {
            using (var dc = this.GetCell(x, y).RenderOpen())
            {
                GlyphRun gr;
                try
                {
                    gr = new GlyphRun(this.typeface, 0, false, this.fontSize, (float)this.dpiInfo.PixelsPerDip, new[] { this.typeface.CharacterToGlyphMap[c] },
                        this.baselineOrigin, new[] { 0.0 }, new[] { new Point(0, 0) }, null, null, null, null, null);
                }
                catch (KeyNotFoundException)
                {
                    gr = new GlyphRun(this.typeface, 0, false, this.fontSize, (float)this.dpiInfo.PixelsPerDip, new[] { this.typeface.CharacterToGlyphMap[0] },
                        this.baselineOrigin, new[] { 0.0 }, new[] { new Point(0, 0) }, null, null, null, null, null);
                }

                dc.DrawRectangle(!invert ? Brushes.Black : Brushes.Gray, null, new Rect(new Point(0, 0), new Point(this.cellWidth, this.cellHeight)));
                dc.DrawGlyphRun(!invert ? Brushes.Gray : Brushes.Black, gr);
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }

        private void Redraw()
        {
            for (var x = 0; x < this.Console.Width; ++x)
            {
                for (var y = 0; y < this.Console.Height; ++y)
                {
                    this.SetCellCharacter(x, y, (char)this.characters[x, y].Glyph);
                }
            }
        }
    }
}
