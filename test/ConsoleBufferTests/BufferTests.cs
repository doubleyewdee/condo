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
                    Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, c.BasicForegroundColor);
                    Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, c.BasicBackgroundColor);
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
                if (c.Glyph == 'b') Assert.IsTrue(c.ForegroundBright);
                if (c.Glyph == 'n') Assert.IsFalse(c.ForegroundBright);
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
                        Assert.AreEqual(fg > 7, c.ForegroundBright);
                        Assert.AreEqual(bg > 7, c.BackgroundBright);
                        Assert.AreEqual((ConsoleBuffer.Commands.SetGraphicsRendition.Colors)(fg > 7 ? fg - 8 : fg), c.BasicForegroundColor);
                        Assert.AreEqual((ConsoleBuffer.Commands.SetGraphicsRendition.Colors)(bg > 7 ? bg - 8 : bg), c.BasicBackgroundColor);
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
                    Assert.IsTrue(c.ForegroundXterm256);
                    Assert.AreEqual(i, c.ForegroundXterm256Index);
                };
                buffer.Render(surface);

                buffer.AppendString($"\x1b[2J\x1b[H\x1b[48;5;{i}mc");
                surface.OnChar = (c, x, y) =>
                {
                    if (c.Glyph != 'c') return;
                    Assert.IsTrue(c.BackgroundXterm256);
                    Assert.AreEqual(i, c.BackgroundXterm256Index);
                };
                buffer.Render(surface);
            }
        }
    }
}
