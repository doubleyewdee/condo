namespace ConsoleBuffer
{
    using System;
    using System.Text;

    // XXX: Gonna end up with a lot of these and they're really freakin' big.
    // could consider a morphable type with different sizes to avoid the (currently) 12 bytes-per-character issue.
    // on a 'normal' 80x25 terminal the current buffer alone is just >23kB. A 160 character wide buffer with a 32k
    // line scrollback is nearly 60MB. Per buffer. Not an issue now but something we should care about and fix in
    // the future.
    public struct Character : IEquatable<Character>
    {
        public struct ColorInfo : IEquatable<ColorInfo>
        {
            public byte R;
            public byte G;
            public byte B;

            public ColorInfo(string rgbValue)
            {
                this.R = this.G = this.B = 0;
                if (rgbValue.Length != 7 || rgbValue[0] != '#')
                {
                    throw new ArgumentException(nameof(rgbValue));
                }
                for (var i = 1; i < 7; ++i)
                {
                    if (rgbValue[i] < '0' || rgbValue[i] > '9')
                        throw new ArgumentException(nameof(rgbValue));
                }
            }

            public bool Equals(ColorInfo other)
            {
                return (this.R == other.R && this.G == other.G && this.B == other.B);
            }

            public static bool operator ==(ColorInfo c1, ColorInfo c2)
            {
                return c1.Equals(c2);
            }

            public static bool operator !=(ColorInfo c1, ColorInfo c2)
            {
                return !c1.Equals(c2);
            }

            public override bool Equals(object obj)
            {
                return obj is ColorInfo other ? other.Equals(this) : false;
            }

            public override int GetHashCode()
            {
                return (this.R + this.G << 8 + this.B << 16);
            }

            public override string ToString()
            {
                return $"#{this.R:x2}{this.G:x2}{this.B:x2}";
            }
        }

        public ColorInfo Foreground { get; set; }
        public ColorInfo Background { get; set; }

        // traditional colors occupy 3 bits, we keep two sets (foreground + background).
        // the actual colors are declared in the SGR command.
        internal const short ForegroundColorMask = 0x0007;
        private const short BackgroundBitShift = 3;
        internal const short BackgroundColorMask = ForegroundColorMask << BackgroundBitShift;
        // flags
        internal const short ForegroundBasicColorFlag = 0x0001 << 6;
        internal const short BackgroundBasicColorFlag = 0x0002 << 6;
        internal const short ForegroundBrightFlag = 0x0004 << 6;
        internal const short BackgroundBrightFlag = 0x0008 << 6;
        internal const short UnderlineFlag = 0x0010 << 6;
        internal const short InverseFlag = 0x0020 << 6;
        internal const short ForegroundExtendedFlag = 0x0040 << 6;
        internal const short BackgroundExtendedFlag = 0x0080 << 6;

        internal const short DefaultOptions = (0x7 | ForegroundBasicColorFlag | BackgroundBasicColorFlag);

        internal short Options;

        internal static short GetColorFlags(Commands.SetGraphicsRendition.Colors color, bool background)
        {
            var options = (short)color;
#if DEBUG
            if (options < (short)Commands.SetGraphicsRendition.Colors.Black || options > (short)Commands.SetGraphicsRendition.Colors.White)
            {
                throw new ArgumentOutOfRangeException(nameof(color));
            }
#endif

            return background ? (short)(options << BackgroundBitShift) : options;
        }

        internal Commands.SetGraphicsRendition.Colors BasicForegroundColor => (Commands.SetGraphicsRendition.Colors)(this.Options & ForegroundColorMask);
        internal bool HasBasicForegroundColor => (this.Options & ForegroundBasicColorFlag) != 0;
        internal Commands.SetGraphicsRendition.Colors BasicBackgroundColor => (Commands.SetGraphicsRendition.Colors)((this.Options & BackgroundColorMask) >> BackgroundBitShift);
        internal bool HasBasicBackgroundColor => (this.Options & BackgroundBasicColorFlag) != 0;
        internal bool ForegroundBright => (this.Options & ForegroundBrightFlag) != 0;
        internal bool BackgroundBright => (this.Options & BackgroundBrightFlag) != 0;
        // this is the only property we cannot handle rendering of internally by setting appropriate RGB color values.
        public bool Underline => (this.Options & UnderlineFlag) != 0;
        internal bool Inverse => (this.Options & InverseFlag) != 0;
        internal bool ForegroundExtended => (this.Options & ForegroundExtendedFlag) != 0;
        internal bool BackgroundExtended => (this.Options & BackgroundExtendedFlag) != 0;

        /// <summary>
        /// The unicode glyph for this character.
        /// </summary>
        public int Glyph { get; set; } // XXX: a single int isn't sufficient to represent emoji with ZWJ. fix later.

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"'{(char)this.Glyph}' (opt:");
            if (this.ForegroundBright) sb.Append(" bright");
            if (this.Underline) sb.Append(" ul");
            if (this.Inverse) sb.Append(" inv");
            if (this.BackgroundBright) sb.Append(" bgBright");
            if (this.HasBasicForegroundColor) sb.Append($" fg:{this.BasicForegroundColor}");
            if (this.HasBasicBackgroundColor) sb.Append($" bg:{this.BasicBackgroundColor}");
            if (this.ForegroundExtended) sb.Append($" efg:#{this.Foreground.R:x2}{this.Foreground.G:x2}{this.Foreground.B:x2}");
            if (this.BackgroundExtended) sb.Append($" ebg:#{this.Background.R:x2}{this.Background.G:x2}{this.Background.B:x2}");
            sb.Append(')');
            return sb.ToString();
        }

        public bool Equals(Character other)
        {
            return this.Foreground == other.Foreground && this.Background == other.Background && this.Glyph == other.Glyph;
        }

        public static bool operator ==(Character c1, Character c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Character c1, Character c2)
        {
            return !c1.Equals(c2);
        }

        public override bool Equals(object obj)
        {
            return obj is Character other ? other.Equals(this) : false;
        }

        public override int GetHashCode()
        {
            var hash = 5309 * this.Glyph;
            hash = hash * 47 + this.Foreground.GetHashCode();
            hash = hash * 47 + this.Background.GetHashCode();
            return hash;
        }
    }
}
