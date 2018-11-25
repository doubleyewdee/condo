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
        [Flags]
        public enum Options : byte
        {
            None = 0x00,
            // we have 3 color bits which combine to represent the 8 "basic" colors of classic terminals
            ColorBits = 0x07, // (0x01, 0x02, 0x04)
            Black = 0x00,
            Red = 0x01,
            Green = 0x02,
            Yellow = 0x03,
            Blue = 0x04,
            Magenta = 0x05,
            Cyan = 0x06,
            White = 0x07,

            // we use 3 additional bits (currently) for flags
            FlagBits = 0x38, // (0x08, 0x10, 0x20)
            Bright = 0x08,
            Underline = 0x10,
            Inverse = 0x20,
            Extended = 0x18,
            // 0x28 free
            // 0x38 free

            // 0x40, 0x80 free bits
        }

        public struct ColorInfo
        {
            public byte R;
            public byte G;
            public byte B;
            public Options Options;

            public bool Bright
            {
                get
                {
                    return ((byte)this.Options & (byte)Options.Bright) == (byte)Options.Bright;
                }
                set
                {
                    if (value)
                    {
                        this.Options = (Options)((byte)this.Options | (byte)Options.Bright);
                    }
                    else
                    {
                        this.Options = (Options)((byte)this.Options & ~(byte)Options.Bright);
                    }
                }
            }

            /// <summary>
            /// Whether the underline bit is set. Should not apply to background colors.
            /// </summary>
            public bool Underline
            {
                get
                {
                    return ((byte)this.Options & (byte)Options.Underline) == (byte)Options.Underline;
                }
                set
                {
                    if (value)
                    {
                        this.Options = (Options)((byte)this.Options | (byte)Options.Underline);
                    }
                    else
                    {
                        this.Options = (Options)((byte)this.Options & ~(byte)Options.Underline);
                    }
                }
            }

            /// <summary>
            /// Whether the 'inverse' bit is set. Should not apply to background colors.
            /// </summary>
            public bool Inverse
            {
                get
                {
                    return ((byte)this.Options & (byte)Options.Inverse) == (byte)Options.Inverse;
                }
                set
                {
                    if (value)
                    {
                        this.Options = (Options)((byte)this.Options | (byte)Options.Inverse);
                    }
                    else
                    {
                        this.Options = (Options)((byte)this.Options & ~(byte)Options.Inverse);
                    }
                }
            }

            /// <summary>
            /// Whether extended (RGB) color values should be applied for this cell.
            /// </summary>
            public bool Extended
            {
                get
                {
                    return ((byte)this.Options & (byte)Options.Extended) == (byte)Options.Extended;
                }
                set
                {
                    if (value)
                    {
                        this.Options = (Options)((byte)this.Options | (byte)Options.Extended);
                    }
                    else
                    {
                        this.Options = (Options)((byte)this.Options & ~(byte)Options.Extended);
                    }
                }
            }

            /// <summary>
            /// Classic color value.
            /// </summary>
            public Options BasicColor
            {
                get
                {
                    return (Options)((byte)this.Options & (byte)Options.ColorBits);
                }
                set
                {
                    var colorValue = (byte)value & (byte)Options.ColorBits;
                    var colorlessOptions = (byte)this.Options & ~(byte)Options.ColorBits;
                    this.Options = (Options)(colorlessOptions & colorValue);
                }
            }

            /// <summary>
            /// True if there is any classic color value set.
            /// </summary>
            public bool HasBasicColor => ((byte)this.Options & (byte)Options.ColorBits) != 0;
        }

        public ColorInfo Foreground { get; set; }
        public ColorInfo Background { get; set; }

        public int Glyph { get; set; } // XXX: a single int isn't sufficient to represent emoji with ZWJ. fix later.
    }
}
