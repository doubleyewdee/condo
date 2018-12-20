namespace ConsoleBuffer.Commands
{
    public sealed class SetMode : ControlSequence
    {
        /// <summary>
        /// True if the command was to set (not reset) the parameter.
        /// </summary>
        public bool Set { get; private set; }
        public enum Parameter
        {
            CursorShow = 0,
            CursorBlink,
            Unknown,
        }

        public Parameter Setting { get; private set; }

        public SetMode(string bufferData, bool set) : base(bufferData)
        {
            this.Set = set;
            this.Setting = Parameter.Unknown;

            if (!this.IsExtended)
            {
                return;
            }

            if (this.Parameters.Count == 1)
            {
                switch (this.Parameters.GetValue(0))
                {
                case 12:
                    this.Setting = Parameter.CursorBlink;
                    break;
                case 25:
                    this.Setting = Parameter.CursorShow;
                    break;
                }
            }
        }
    }
}
