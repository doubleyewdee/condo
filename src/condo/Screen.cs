using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace condo
{
    public sealed class Screen : FrameworkElement
    {
        private VisualCollection cells;
        private DpiScale dpiInfo;
        private readonly GlyphTypeface typeface;
        private readonly int fontSize = 14;
        private readonly double cellWidth, cellHeight;
        private readonly Point baselineOrigin;
        private readonly Rect cellRectangle;
        private int horizontalCells, verticalCells;

        public Screen() : this(80, 25) { }

        public Screen(int width, int height)
        {
            this.cells = new VisualCollection(this);
            if (!new Typeface("Consolas").TryGetGlyphTypeface(out this.typeface))
            {
                throw new InvalidOperationException("Could not get desired font.");
            }

            this.horizontalCells = width;
            this.verticalCells = height;

            this.cellWidth = this.typeface.AdvanceWidths[0] * this.fontSize;
            this.cellHeight = this.typeface.Height * this.fontSize;
            this.baselineOrigin = new Point(0, this.typeface.Baseline * this.fontSize);
            this.cellRectangle = new Rect(new Size(this.cellWidth, this.cellHeight));

            this.dpiInfo = VisualTreeHelper.GetDpi(this);
            this.Resize();
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

        public void SetCellCharacter(int x, int y, char c)
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

                dc.DrawRectangle(Brushes.Black, null, new Rect(new Point(0, 0), new Point(this.cellWidth, this.cellHeight)));
                dc.DrawGlyphRun(Brushes.Gray, gr);
            }
        }
    }
}
