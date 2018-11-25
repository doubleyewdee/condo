namespace ConsoleBufferTests
{
    using System;
    using ConsoleBuffer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class XtermPaletteTests
    {
        [TestMethod]
        public void Expect256Colors()
        {
            var palette = new XtermPalette();
            for (var i = 0; i < 256; ++i)
            {
                Assert.IsInstanceOfType(palette[i], typeof(Character.ColorInfo));
            }
            Assert.ThrowsException<IndexOutOfRangeException>(() => palette[256]);
        }

        [TestMethod]
        [DataRow("black", 0, 0, 0)]
        [DataRow("WHITE", 0xff, 0xff, 0xff)]
        [DataRow("oraNGE4", 0x87, 0x5f, 0x00)]
        [DataRow("Purple3", 0x5f, 0x00, 0xd7)]
        public void LookupColorByName(string name, int rValue, int gValue, int bValue)
        {
            var palette = new XtermPalette();
            var colorInfo = palette[name];
            Assert.AreEqual(rValue, colorInfo.R);
            Assert.AreEqual(gValue, colorInfo.G);
            Assert.AreEqual(bValue, colorInfo.B);
        }
    }
}
