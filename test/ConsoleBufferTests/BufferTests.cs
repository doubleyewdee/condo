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
            buffer.AppendString("\x1b[1mhello\n");
            var surface = new RenderTest();
            surface.OnChar = (c, x, y) =>
            {
                if (c.Glyph != 0x20)
                {
                    Assert.AreEqual(buffer.Palette["white"], c.Foreground);
                }
            };
            buffer.Render(surface);
        }
    }
}
