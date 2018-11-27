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

        public bool HaveBasicForeground { get; private set; }
        public Colors BasicForegroundColor { get; private set; } = Colors.None;
        public bool HaveForeground { get; private set; }
        public Character.ColorInfo ForegroundColor { get; private set; }

        public bool HaveBasicBackground { get; private set; }
        public Colors BasicBackgroundColor { get; private set; } = Colors.None;
        public bool HaveBackground { get; private set; }
        public Character.ColorInfo BackgroundColor { get; private set; }

        public FlagValue ForegroundBright { get; private set; }
        public FlagValue BackgroundBright { get; private set; }
        public FlagValue Underline { get; private set; }
        public FlagValue Inverse { get; private set; }

        public SetGraphicsRendition(string bufferData) : base(bufferData)
        {
#if DEBUG
            // XXX: remove later.
            Trace.WriteLine($"SGR: ^[[{bufferData}m");
#endif

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
                var pValue = this.ParameterToNumber(p, defaultValue: -1);
                switch (pValue)
                {
                case 0:
                    this.SetDefault();
                    break;
                case 1:
                    this.ForegroundBright = FlagValue.Set;
                    break;
                case 2:
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

        private void SetDefault()
        {
            this.ForegroundBright = FlagValue.Unset;
            this.BackgroundBright = FlagValue.Unset;
            this.Underline = FlagValue.Unset;
            this.Inverse = FlagValue.Unset;
            this.HaveForeground = this.HaveBackground = false;
            this.HaveBasicForeground = this.HaveBasicBackground = true;
            this.BasicForegroundColor = Colors.White;
            this.BasicBackgroundColor = Colors.Black;
        }

        public override string ToString()
        {
            return $"^[[{string.Join(";", this.Parameters)}m";
        }
    }
}