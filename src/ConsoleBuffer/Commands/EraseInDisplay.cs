using System;

namespace ConsoleBuffer.Commands
{
    public sealed class EraseInDisplay : ControlSequence
    {
        public enum Parameter
        {
            Below = 0,
            Above,
            All,
            // xterm supports '3' for saved lines.
            Unknown,
        }

        public Parameter Direction { get; private set; }

        public EraseInDisplay(string bufferData) : base(bufferData)
        {
            this.Direction = Parameter.Unknown;
            if (this.IsExtended)
            {
                return; // extended means selective erase which we don't support.
            }
            if (this.Parameters.Count == 0)
            {
                this.Direction = Parameter.Below;
            }
            if (this.Parameters.Count == 1 && uint.TryParse(this.Parameters[0], out var param))
            {
                param = Math.Min((uint)Parameter.Unknown, param);
                this.Direction = (Parameter)param;
            }
        }
    }
}
