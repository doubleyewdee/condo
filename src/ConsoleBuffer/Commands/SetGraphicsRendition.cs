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

        public bool HaveForeground { get; private set; }
        public Character.ColorInfo ForegroundColor { get; private set; }

        public bool HaveBackground { get; private set; }
        public Character.ColorInfo BackgroundColor { get; private set; }

        public FlagValue Bold { get; private set; }
        public FlagValue Underline { get; private set; }
        public FlagValue Inverse { get; private set; }

        public SetGraphicsRendition(string bufferData) : base(bufferData)
        {
            this.Bold = FlagValue.None;
            this.Underline = FlagValue.None;
            this.Inverse = FlagValue.None;
        }
    }
}
