namespace ConsoleBuffer.Commands
{
    using System;

    public sealed class EraseIn : ControlSequence
    {
        public enum Parameter
        {
            Before = 0,
            After,
            All,
            // xterm supports '3' for saved lines.
            Unknown,
        }

        public enum EraseType
        {
            Line = 0,
            Display,
        }

        public Parameter Direction { get; private set; }
        public EraseType Type { get; private set; }

        public EraseIn(string bufferData, EraseType type) : base(bufferData)
        {
            this.Direction = Parameter.Unknown;
            this.Type = type;
            if (this.IsExtended)
            {
                return; // extended means selective erase which we don't support.
            }
            if (this.Parameters.Count == 0)
            {
                this.Direction = Parameter.Before;
            }

            if (this.Parameters.Count == 1)
            {   
                var param = Math.Min((uint)Parameter.Unknown, (uint)this.Parameters.GetValue(0));
                this.Direction = (Parameter)param;
            }
        }
    }
}
