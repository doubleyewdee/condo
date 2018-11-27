namespace ConsoleBufferTests
{
    using System;
    using ConsoleBuffer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class BufferTests
    {
        private const int DefaultColumns = 80;
        private const int DefaultRows = 25;

        private sealed class RenderTest : IRenderTarget
        {
            public Action<Character, int, int> OnChar;

            public void RenderCharacter(Character c, int x, int y)
            {
                this.OnChar(c, x, y);
            }
        }

        [TestMethod]
        public void InitializeState()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            Assert.AreEqual(DefaultColumns, buffer.Width);
            Assert.AreEqual(DefaultRows, buffer.Height);
            Assert.AreEqual((0, 0), buffer.CursorPosition);
            Assert.IsTrue(buffer.CursorVisible);
            Assert.IsTrue(buffer.CursorBlink);
            Assert.AreEqual(string.Empty, buffer.Title);
            Assert.AreEqual(DefaultRows, buffer.BufferSize);
            Assert.AreEqual(XtermPalette.Default, buffer.Palette);
        }

        [TestMethod]
        public void DefaultColors()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            buffer.AppendString("!");
            var surface = new RenderTest();
            surface.OnChar = (c, x, y) =>
            {
                if (x == 0 && y == 0)
                {
                    Assert.AreEqual(XtermPalette.Default["silver"], c.Foreground);
                    Assert.AreEqual(XtermPalette.Default["black"], c.Background);
                }
            };
            buffer.Render(surface);
        }

        [TestMethod]
        public void MaxBufferSize()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            for (var i = 0;i < short.MaxValue; ++i)
            {
                buffer.AppendString($"line {i}\r\n");
            }
            Assert.AreEqual(short.MaxValue, buffer.BufferSize);

            buffer.AppendString($"that's too much, man! -sarah lynn (1984 - 2016)\r\n");
            Assert.AreEqual(short.MaxValue, buffer.BufferSize);
        }

        [TestMethod]
        public void BrightForegroundText()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            buffer.AppendString("\x1b[1mbb\x1b[22mn\x1b[1mb\x1b[22mnnnn\n");
            var surface = new RenderTest();
            surface.OnChar = (c, x, y) =>
            {
                if (c.Glyph == 'b') Assert.AreEqual(buffer.Palette["white"], c.Foreground);
                if (c.Glyph == 'n') Assert.AreEqual(buffer.Palette["silver"], c.Foreground);
            };
            buffer.Render(surface);
        }

        [TestMethod]
        public void BasicColor()
        {
            var surface = new RenderTest();
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);

            for (var fg = 0; fg < 16; ++fg)
            {
                for (var bg = 0; bg < 16; ++bg)
                {
                    buffer.AppendString("\x1b[2J\x1b[m");
                    buffer.AppendString("\x1b[");
                    if (fg < 8) buffer.AppendString($"3{fg}");
                    else buffer.AppendString($"9{fg - 8}");
                    if (bg < 8) buffer.AppendString($";4{bg}m");
                    else buffer.AppendString($";10{bg - 8}m");
                    buffer.AppendString("c\r\n");

                    surface.OnChar = (c, x, y) =>
                    {
                        if (c.Glyph != 'c') return;
                        Assert.AreEqual(buffer.Palette[fg], c.Foreground);
                        Assert.AreEqual(buffer.Palette[bg], c.Background);
                    };

                    buffer.Render(surface);
                }
            }
        }

        [TestMethod]
        public void XtermColorIndex()
        {
            var surface = new RenderTest();
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);

            for (var i = 0;i < 256; ++i)
            {
                buffer.AppendString($"\x1b[2J\x1b[H\x1b[38;5;{i}mc");
                surface.OnChar = (c, x, y) =>
                {
                    if (c.Glyph != 'c') return;
                    Assert.AreEqual(buffer.Palette[i], c.Foreground);
                };
                buffer.Render(surface);

                buffer.AppendString($"\x1b[2J\x1b[H\x1b[48;5;{i}mc");
                surface.OnChar = (c, x, y) =>
                {
                    if (c.Glyph != 'c') return;
                    Assert.AreEqual(buffer.Palette[i], c.Background);
                };
                buffer.Render(surface);
            }
        }
    }
}
