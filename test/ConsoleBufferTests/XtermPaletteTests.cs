namespace ConsoleBufferTests
{
    using System;
    using ConsoleBuffer;
    using Xunit;

    public class XtermPaletteTests
    {
        [Fact]
        public void Expect256Colors()
        {
            var palette = new XtermPalette();
            for (var i = 0; i < 256; ++i)
            {
                Assert.IsType<Character.ColorInfo>(palette[i]);
            }
            Assert.Throws<IndexOutOfRangeException>(() => palette[256]);
        }

        [Theory]
        [InlineData("black", 0, 0, 0)]
        [InlineData("WHITE", 0xff, 0xff, 0xff)]
        [InlineData("oraNGE4", 0x87, 0x5f, 0x00)]
        [InlineData("Purple3", 0x5f, 0x00, 0xd7)]
        public void LookupColorByName(string name, int rValue, int gValue, int bValue)
        {
            var palette = new XtermPalette();
            var colorInfo = palette[name];
            Assert.Equal(rValue, colorInfo.R);
            Assert.Equal(gValue, colorInfo.G);
            Assert.Equal(bValue, colorInfo.B);
        }
    }
}
