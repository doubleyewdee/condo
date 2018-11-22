namespace ConsoleBuffer.Commands
{
    public sealed class SetCursorPosition : ControlSequence
    {
        /// <summary>
        /// Will be 0 for left-most position, -1 if X axis should not be applied.
        /// </summary>
        public int PosX { get; private set; }
        /// <summary>
        /// Will be 0 for top-most position, -1 if Y axis should not be applied.
        /// </summary>
        public int PosY { get; private set; }

        public SetCursorPosition(string bufferData, char cmd) : base(bufferData)
        {
            this.PosX = this.PosY = -1;
            switch (cmd)
            {
            case 'G':
                this.PosX = this.ParameterToNumber(0, defaultValue: 1) - 1;
                break;
            case 'd':
                this.PosY = this.ParameterToNumber(0, defaultValue: 1) - 1;
                break;
            case 'H':
            case 'f':
                this.PosY = this.ParameterToNumber(0, defaultValue: 1) - 1;
                this.PosX = this.ParameterToNumber(1, defaultValue: 1) - 1;
                break;
            }
        }
    }
}
