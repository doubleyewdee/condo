namespace ConsoleBuffer.Commands
{
    using System;

    public sealed class CursorMove : ControlSequence
    {
        public enum CursorDirection
        {
            Up = 0,
            Down,
            Forward,
            Backward,
        }

        public CursorDirection Direction { get; private set; }
        public int Count { get; private set; }

        public CursorMove(string bufferData, char cmd) : base(bufferData)
        {
            if (cmd < 'A' || cmd > 'D')
            {
                throw new ArgumentOutOfRangeException(nameof(cmd));
            }
            this.Direction = (CursorDirection)(cmd - 'A');
            this.Count = this.ParameterToNumber(0, defaultValue: 1);
        }
    }
}
