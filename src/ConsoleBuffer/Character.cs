namespace ConsoleBuffer
{
    // XXX: Gonna end up with a lot of these and they're really freakin' big.
    // could consider a morphable type with different sizes to avoid the (currently) 12 bytes-per-character issue.
    // on a 'normal' 80x25 terminal the current buffer alone is just >23kB. A 160 character wide buffer with a 32k
    // line scrollback is nearly 60MB. Per buffer. Not an issue now but something we should care about and fix in
    // the future.
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
        public int Glyph { get; set; } // XXX: a single int isn't quite sufficient to represent emoji with ZWJ. fix later.
    }
}
