namespace ConsoleBufferTests
{
    using System;
    using ConsoleBuffer;
    using Xunit;

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

        [Fact]
        public void InitializeState()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            Assert.Equal(DefaultColumns, buffer.Width);
            Assert.Equal(DefaultRows, buffer.Height);
            Assert.Equal((0, 0), buffer.CursorPosition);
            Assert.True(buffer.CursorVisible);
            Assert.True(buffer.CursorBlink);
            Assert.Equal(string.Empty, buffer.Title);
            Assert.Equal(DefaultRows, buffer.BufferSize);
        }

        [Fact]
        public void DefaultColors()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            buffer.AppendString("!");
            var surface = new RenderTest();
            surface.OnChar = (c, x, y) =>
            {
                if (x == 0 && y == 0)
                {
                    Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, c.BasicForegroundColor);
                    Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, c.BasicBackgroundColor);
                }
            };
            buffer.Render(surface);
        }

        [Fact]
        public void MaxBufferSize()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            for (var i = 0;i < short.MaxValue; ++i)
            {
                buffer.AppendString($"line {i}\r\n");
            }
            Assert.Equal(short.MaxValue, buffer.BufferSize);

            buffer.AppendString($"that's too much, man! -sarah lynn (1984 - 2016)\r\n");
            Assert.Equal(short.MaxValue, buffer.BufferSize);
        }

        [Fact]
        public void BrightForegroundText()
        {
            var buffer = new ConsoleBuffer.Buffer(DefaultColumns, DefaultRows);
            buffer.AppendString("\x1b[1mbb\x1b[22mn\x1b[1mb\x1b[22mnnnn\n");
            var surface = new RenderTest();
            surface.OnChar = (c, x, y) =>
            {
                if (c.Glyph == 'b') Assert.True(c.ForegroundBright);
                if (c.Glyph == 'n') Assert.False(c.ForegroundBright);
            };
            buffer.Render(surface);
        }

        [Fact]
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
                        Assert.Equal(fg > 7, c.ForegroundBright);
                        Assert.Equal(bg > 7, c.BackgroundBright);
                        Assert.Equal((ConsoleBuffer.Commands.SetGraphicsRendition.Colors)(fg > 7 ? fg - 8 : fg), c.BasicForegroundColor);
                        Assert.Equal((ConsoleBuffer.Commands.SetGraphicsRendition.Colors)(bg > 7 ? bg - 8 : bg), c.BasicBackgroundColor);
                    };

                    buffer.Render(surface);
                }
            }
        }

        [Fact]
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
                    Assert.True(c.ForegroundXterm256);
                    Assert.Equal(i, c.ForegroundXterm256Index);
                };
                buffer.Render(surface);

                buffer.AppendString($"\x1b[2J\x1b[H\x1b[48;5;{i}mc");
                surface.OnChar = (c, x, y) =>
                {
                    if (c.Glyph != 'c') return;
                    Assert.True(c.BackgroundXterm256);
                    Assert.Equal(i, c.BackgroundXterm256Index);
                };
                buffer.Render(surface);
            }
        }
    }
}
