using System;

namespace ConsoleBuffer.Commands
{
    public sealed class EraseCharacter : ControlSequence
    {
        public int Count;

        public EraseCharacter(string bufferData) : base(bufferData)
        {
            this.Count = 1;
            // NB: if the user provides an invalid parameter we still choose to erase one character.
            if (this.Parameters.Count == 1 && ushort.TryParse(this.Parameters[0], out var count))
            {
                this.Count = Math.Max(1, (int)count); // 0 means scroll 1.
            }
        }
    }
}
