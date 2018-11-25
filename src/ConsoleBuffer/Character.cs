namespace ConsoleBuffer
{
    using System;

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
        }

        public ColorInfo Foreground { get; set; }
        public ColorInfo Background { get; set; }

        // traditional colors occupy 3 bits, we keep two sets (foreground + background).
        public const short Black = 0x0000;
        public const short Red = 0x0001;
        public const short Green = 0x0002;
        public const short Yellow = 0x0003;
        public const short Blue = 0x0004;
        public const short Magenta = 0x0005;
        public const short Cyan = 0x0006;
        public const short White = 0x0007;
        private const short ForegroundColorMask = 0x0007;
        private const short BackgroundBitShift = 3;
        private const short BackgroundColorMask = ForegroundColorMask << BackgroundBitShift;
        // flags
        private const short ForegroundBasicColorFlag = 0x0001 << 6;
        private const short BackgroundBasicColorFlag = 0x0002 << 6;
        private const short ForegroundBrightFlag = 0x0004 << 6;
        private const short BackgroundBrightFlag = 0x0008 << 6;
        private const short UnderlineFlag = 0x0010 << 6;
        private const short InverseFlag = 0x0020 << 6;
        private const short ForegroundExplicitFlag = 0x0040 << 6;
        private const short BackgroundExplicitFlag = 0x0080 << 6;
        private const short ExplicitFlags = (ForegroundExplicitFlag | BackgroundBrightFlag);
        private const short ForegroundExtendedFlag = 0x0100 << 6;
        private const short BackgroundExtendedFlag = unchecked((short)(0x0200 << 6));

        public short Options;

        public static short BasicColorOptions(short foreground = -1, short background = -1)
        {
            short options = 0;
            if (foreground > -1)
            {
#if DEBUG
                if (foreground > White)
                {
                    throw new ArgumentOutOfRangeException(nameof(foreground));
                }
#endif
                options |= (short)(foreground | ForegroundExplicitFlag | ForegroundBasicColorFlag);
            }
            if (background > -1)
            {
#if DEBUG
                if (background > White)
                {
                    throw new ArgumentOutOfRangeException(nameof(background));
                }
#endif
                options |= (short)((background << BackgroundBitShift) | BackgroundExplicitFlag | BackgroundBasicColorFlag);
            }

            return options;
        }

        public short ForegroundColor => (short)(this.Options & ForegroundColorMask);
        public bool HasBasicForegroundColor => (this.Options & ForegroundBasicColorFlag) != 0;
        public short BackgroundColor => (short)(this.Options & BackgroundColorMask);
        public bool HasBasicBackgroundColor => (this.Options & BackgroundBasicColorFlag) != 0;
        public bool ForegroundBright => (this.Options & ForegroundBrightFlag) != 0;
        public bool BackgroundBright => (this.Options & BackgroundBrightFlag) != 0;
        public bool Underline => (this.Options & UnderlineFlag) != 0;
        public bool Inverse => (this.Options & InverseFlag) != 0;
        public bool ForegroundExplicit => (this.Options & ForegroundExplicitFlag) != 0;
        public bool BackgroundExplicit => (this.Options & BackgroundExplicitFlag) != 0;
        public bool ForegroundExtended => (this.Options & ForegroundExtendedFlag) != 0;
        public bool BackgroundExtended => (this.Options & BackgroundExtendedFlag) != 0;

        public short InheritedOptions => (short)(this.Options & ~(ForegroundExplicitFlag | BackgroundExplicitFlag));

        /// <summary>
        /// The unicode glyph for this character.
        /// </summary>
        public int Glyph { get; set; } // XXX: a single int isn't sufficient to represent emoji with ZWJ. fix later.

        public Character(Character parent)
        {
            this.Foreground = parent.Foreground;
            this.Background = parent.Background;
            this.Glyph = parent.Glyph;
            this.Options = parent.Options;
            this.Options &= ~ExplicitFlags;
        }
    }
}
