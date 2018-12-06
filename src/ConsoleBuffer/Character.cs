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
        internal const short ForegroundBasicColorFlag = 0x0008;
        private const short BackgroundBitShift = 4;
        internal const short BackgroundColorMask = ForegroundColorMask << BackgroundBitShift;
        // flags
        internal const short BackgroundBasicColorFlag = 0x0080;
        internal const short ForegroundBrightFlag = 0x0100;
        internal const short BackgroundBrightFlag = 0x0200;
        internal const short UnderlineFlag = 0x0400;
        internal const short InverseFlag = 0x0800;
        internal const short ForegroundXterm256Flag = 0x1000;
        internal const short BackgroundXterm256Flag = 0x2000;
        internal const short ForegroundRGBFlag = 0x4000;
        internal const short BackgroundRGBFlag = unchecked((short)0x8000);

        internal const short ForegroundColorFlags = (ForegroundBasicColorFlag | ForegroundXterm256Flag | ForegroundRGBFlag | ForegroundColorMask);
        internal const short BackgroundColorFlags = (BackgroundBasicColorFlag | BackgroundXterm256Flag | BackgroundRGBFlag | BackgroundColorMask);

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

        public bool ForegroundBright => (this.Options & ForegroundBrightFlag) != 0;
        public bool BackgroundBright => (this.Options & BackgroundBrightFlag) != 0;
        public bool Underline => (this.Options & UnderlineFlag) != 0;
        public bool Inverse => (this.Options & InverseFlag) != 0;

        public bool ForegroundBasic => (this.Options & ForegroundBasicColorFlag) != 0;
        public Commands.SetGraphicsRendition.Colors BasicForegroundColor =>
            this.ForegroundBasic ? (Commands.SetGraphicsRendition.Colors)(this.Options & ForegroundColorMask) : Commands.SetGraphicsRendition.Colors.None;
        public bool BackgroundBasic => (this.Options & BackgroundBasicColorFlag) != 0;
        public Commands.SetGraphicsRendition.Colors BasicBackgroundColor =>
            this.BackgroundBasic ? (Commands.SetGraphicsRendition.Colors)((this.Options & BackgroundColorMask) >> BackgroundBitShift) : Commands.SetGraphicsRendition.Colors.None;
        public bool ForegroundXterm256 => (this.Options & ForegroundXterm256Flag) != 0;
        public int ForegroundXterm256Index => this.ForegroundXterm256 ? this.Foreground.R : -1;
        public bool BackgroundXterm256 => (this.Options & BackgroundXterm256Flag) != 0;
        public int BackgroundXterm256Index => this.BackgroundXterm256 ? this.Background.R : -1;
        public bool ForegroundRGB => (this.Options & ForegroundRGBFlag) != 0;
        public bool BackgroundRGB => (this.Options & BackgroundRGBFlag) != 0;

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
            if (this.ForegroundBasic) sb.Append($" fg:{this.BasicForegroundColor}");
            if (this.BackgroundBasic) sb.Append($" bg:{this.BasicBackgroundColor}");
            if (this.ForegroundXterm256) sb.Append($" xtfg:{this.Foreground.R}");
            if (this.BackgroundXterm256) sb.Append($" xtbg:{this.Background.R}");
            if (this.ForegroundRGB) sb.Append($" rgbfg:#{this.Foreground.R:x2}{this.Foreground.G:x2}{this.Foreground.B:x2}");
            if (this.BackgroundRGB) sb.Append($" rgbbg:#{this.Background.R:x2}{this.Background.G:x2}{this.Background.B:x2}");
            sb.Append(')');
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if the characters have identical properties, even if the glyphs differ.
        /// </summary>
        public bool PropertiesEqual(Character other)
        {
            return this.Options == other.Options && this.Foreground == other.Foreground && this.Background == other.Background;
        }

        public bool Equals(Character other)
        {
            return this.PropertiesEqual(other) && this.Glyph == other.Glyph;
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
            hash = hash * 47 + this.Options;
            hash = hash * 47 + this.Foreground.GetHashCode();
            hash = hash * 47 + this.Background.GetHashCode();
            return hash;
        }
    }
}
