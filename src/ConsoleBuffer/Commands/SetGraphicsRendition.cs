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
                switch (this.ParameterToNumber(p, defaultValue: -1))
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
    }
}
