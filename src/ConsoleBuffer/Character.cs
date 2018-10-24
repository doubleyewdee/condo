using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBuffer
{
    // XXX: Gonna end up with a lot of these and they're really freakin' big.
    public struct Character
    {
        public struct ColorInfo
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;
        }

        public ColorInfo Foreground { get; set; }
        public ColorInfo Background { get; set; }
        public char Glyph { get; set; } // XXX: char won't cut it for emoji/etc, gonna have to re-do this later!
    }
}
