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
            public override string ToString()
            {
                return $"#{this.R:x2}{this.G:x2}{this.B:x2}";
            }
        }

        public ColorInfo Foreground { get; set; }
        public ColorInfo Background { get; set; }

        // traditional colors occupy 3 bits, we keep two sets (foreground + background).
        // the actual colors are declared in the SGR command.
        private const short ForegroundColorMask = 0x0007;
        private const short BackgroundBitShift = 3;
        private const short BackgroundColorMask = ForegroundColorMask << BackgroundBitShift;
        // flags
        internal const short ForegroundBasicColorFlag = 0x0001 << 6;
        internal const short BackgroundBasicColorFlag = 0x0002 << 6;
        internal const short ForegroundBrightFlag = 0x0004 << 6;
        internal const short BackgroundBrightFlag = 0x0008 << 6;
        internal const short UnderlineFlag = 0x0010 << 6;
        internal const short InverseFlag = 0x0020 << 6;
        internal const short ForegroundExplicitFlag = 0x0040 << 6;
        internal const short BackgroundExplicitFlag = 0x0080 << 6;
        internal const short ExplicitFlags = (ForegroundExplicitFlag | BackgroundBrightFlag);
        internal const short ForegroundExtendedFlag = 0x0100 << 6;
        internal const short BackgroundExtendedFlag = unchecked((short)(0x0200 << 6));

        internal short Options;

        internal static short BasicColorOptions(Commands.SetGraphicsRendition.Colors foreground = Commands.SetGraphicsRendition.Colors.None,
                                                Commands.SetGraphicsRendition.Colors background = Commands.SetGraphicsRendition.Colors.None)
        {
            short options = 0;
            if (foreground != Commands.SetGraphicsRendition.Colors.None)
            {
                options |= (short)((short)foreground | ForegroundExplicitFlag | ForegroundBasicColorFlag);
            }
            if (background != Commands.SetGraphicsRendition.Colors.None)
            {
                options |= (short)(((short)background << BackgroundBitShift) | BackgroundExplicitFlag | BackgroundBasicColorFlag);
            }

            return options;
        }

        internal Commands.SetGraphicsRendition.Colors BasicForegroundColor => (Commands.SetGraphicsRendition.Colors)(this.Options & ForegroundColorMask);
        internal bool HasBasicForegroundColor => (this.Options & ForegroundBasicColorFlag) != 0;
        internal Commands.SetGraphicsRendition.Colors BasicBackgroundColor => (Commands.SetGraphicsRendition.Colors)(this.Options & BackgroundColorMask);
        internal bool HasBasicBackgroundColor => (this.Options & BackgroundBasicColorFlag) != 0;
        internal bool ForegroundBright => (this.Options & ForegroundBrightFlag) != 0;
        internal bool BackgroundBright => (this.Options & BackgroundBrightFlag) != 0;
        internal bool Underline => (this.Options & UnderlineFlag) != 0;
        internal bool Inverse => (this.Options & InverseFlag) != 0;
        internal bool ForegroundExplicit => (this.Options & ForegroundExplicitFlag) != 0;
        internal bool BackgroundExplicit => (this.Options & BackgroundExplicitFlag) != 0;
        internal bool ForegroundExtended => (this.Options & ForegroundExtendedFlag) != 0;
        internal bool BackgroundExtended => (this.Options & BackgroundExtendedFlag) != 0;

        internal short InheritedOptions => (short)(this.Options & ~(ForegroundExplicitFlag | BackgroundExplicitFlag));

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

        public Character(Character parent, Commands.SetGraphicsRendition sgr)
            : this(parent)
        {
            switch (sgr.ForegroundBright)
            {
            case Commands.SetGraphicsRendition.FlagValue.Set:
                this.Options |= (ForegroundBrightFlag | ForegroundExplicitFlag);
                break;
            case Commands.SetGraphicsRendition.FlagValue.Unset:
                this.Options &= ~ForegroundBrightFlag;
                this.Options |= ForegroundExplicitFlag;
                break;
            }

            switch (sgr.BackgroundBright)
            {
            case Commands.SetGraphicsRendition.FlagValue.Set:
                this.Options |= (BackgroundBrightFlag | BackgroundExplicitFlag);
                break;
            case Commands.SetGraphicsRendition.FlagValue.Unset:
                this.Options &= ~BackgroundBrightFlag;
                this.Options |= BackgroundExplicitFlag;
                break;
            }
        }
    }
}
