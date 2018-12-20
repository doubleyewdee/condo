using System.Diagnostics;

namespace ConsoleBuffer.Commands
{
    public sealed class SetGraphicsRendition : ControlSequence
    {
        public enum FlagValue
        {
            Set,
            Unset,
            None,
        }

        public enum Colors : short
        {
            Black = 0,
            Red,
            Green,
            Yellow,
            Blue,
            Magenta,
            Cyan,
            White,
            None,
        }

        public const Colors DefaultForegroundColor = Colors.White;
        public const Colors DefaultBackgroundColor = Colors.Black;

        public bool HaveBasicForeground { get; private set; }
        public Colors BasicForegroundColor { get; private set; } = Colors.None;
        public bool HaveForeground { get; private set; }
        public Character.ColorInfo ForegroundColor { get; private set; }
        public bool HaveXtermForeground { get; private set; }
        public int XtermForegroundColor { get; private set; }

        public bool HaveBasicBackground { get; private set; }
        public Colors BasicBackgroundColor { get; private set; } = Colors.None;
        public bool HaveBackground { get; private set; }
        public Character.ColorInfo BackgroundColor { get; private set; }
        public bool HaveXtermBackground { get; private set; }
        public int XtermBackgroundColor { get; private set; }

        public FlagValue ForegroundBright { get; private set; }
        public FlagValue BackgroundBright { get; private set; }
        public FlagValue Underline { get; private set; }
        public FlagValue Inverse { get; private set; }

        public SetGraphicsRendition(string bufferData) : base(bufferData)
        {
            this.ForegroundBright = FlagValue.None;
            this.BackgroundBright = FlagValue.None;
            this.Underline = FlagValue.None;
            this.Inverse = FlagValue.None;

            if (this.Parameters.Count == 0)
            {
                this.SetDefault();
                return;
            }

            var p = 0;
            while (p < this.Parameters.Count)
            {
                var pValue = this.Parameters.GetValue(p, -1);
                switch (pValue)
                {
                case 0:
                    this.SetDefault();
                    break;
                case 1:
                    this.ForegroundBright = FlagValue.Set;
                    break;
                case 22:
                    this.ForegroundBright = FlagValue.Unset;
                    break;
                case 4:
                    this.Underline = FlagValue.Set;
                    break;
                case 24:
                    this.Underline = FlagValue.Unset;
                    break;
                case 7:
                    this.Inverse = FlagValue.Set;
                    break;
                case 27:
                    this.Inverse = FlagValue.Unset;
                    break;
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                    this.HaveBasicForeground = true;
                    this.BasicForegroundColor = (Colors)(pValue - 30);
                    break;
                case 38:
                {
                    if (this.ReadXtermColorInfo(ref p, out var idx, out var color))
                    {
                        if (idx > -1)
                        {
                            this.HaveXtermForeground = true;
                            this.XtermForegroundColor = idx;
                        }
                        else
                        {
                            this.HaveForeground = true;
                            this.ForegroundColor = color;
                        }
                        break;
                    }
                    this.Reset();
                    return;
                }
                case 39:
                    this.HaveBasicForeground = true;
                    this.BasicForegroundColor = DefaultForegroundColor;
                    break;
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                    this.HaveBasicBackground = true;
                    this.BasicBackgroundColor = (Colors)(pValue - 40);
                    break;
                case 48:
                {
                    if (this.ReadXtermColorInfo(ref p, out var idx, out var color))
                    {
                        if (idx > -1)
                        {
                            this.HaveXtermBackground = true;
                            this.XtermBackgroundColor = idx;
                        }
                        else
                        {
                            this.HaveBackground = true;
                            this.BackgroundColor = color;
                        }
                        break;
                    }
                    this.Reset();
                    return;
                }
                case 49:
                    this.HaveBasicBackground = true;
                    this.BasicBackgroundColor = DefaultBackgroundColor;
                    break;
                case 90:
                case 91:
                case 92:
                case 93:
                case 94:
                case 95:
                case 96:
                case 97:
                    this.HaveBasicForeground = true;
                    this.ForegroundBright = FlagValue.Set; // XXX: idk if this is right
                    this.BasicForegroundColor = (Colors)(pValue - 90);
                    break;
                case 100:
                case 101:
                case 102:
                case 103:
                case 104:
                case 105:
                case 106:
                case 107:
                    this.HaveBasicBackground = true;
                    this.BackgroundBright = FlagValue.Set; // same as above.
                    this.BasicBackgroundColor = (Colors)(pValue - 100);
                    break;
                }

                ++p;
            }
        }

        private bool ReadXtermColorInfo(ref int p, out int value, out Character.ColorInfo color)
        {
            value = -1;
            color = new Character.ColorInfo();
            if (++p < this.Parameters.Count)
            {
                var subCommand = this.Parameters.GetValue(p, -1);
                if (subCommand == 2)
                {
                    var colorValues = new int[3];
                    for (var i = 0; i < colorValues.Length; ++i) // prime candidate for funrolling of loops
                    {
                        ++p;
                        colorValues[i] = this.Parameters.GetValue(p, -1);
                        if (colorValues[i] < 0 || colorValues[i] > 255)
                        {
                            return false;
                        }
                    }
                    color = new Character.ColorInfo { R = (byte)colorValues[0], G = (byte)colorValues[1], B = (byte)colorValues[2] };
                    return true;
                }
                else if (subCommand == 5)
                {
                    ++p;
                    value = this.Parameters.GetValue(p, -1);
                    return value > -1 && value < 256;
                }
            }

            return false;
        }

        private void Reset()
        {
            this.ForegroundBright = this.BackgroundBright = this.Underline = this.Inverse = FlagValue.None;
            this.HaveBasicForeground = this.HaveForeground = this.HaveXtermForeground = false;
            this.HaveBasicBackground = this.HaveBackground = this.HaveXtermBackground = false;
        }

        private void SetDefault()
        {
            this.ForegroundBright = FlagValue.Unset;
            this.BackgroundBright = FlagValue.Unset;
            this.Underline = FlagValue.Unset;
            this.Inverse = FlagValue.Unset;
            this.HaveForeground = this.HaveBackground = false;
            this.HaveBasicForeground = this.HaveBasicBackground = true;
            this.BasicForegroundColor = DefaultForegroundColor;
            this.BasicBackgroundColor = DefaultBackgroundColor;
        }

        public override string ToString()
        {
            return $"^[[{string.Join(";", this.Parameters)}m";
        }
    }
}
