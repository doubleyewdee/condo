using System;

namespace ConsoleBuffer.Commands
{
    public sealed class EraseCharacter : ControlSequence
    {
        public int Count;

        public EraseCharacter(string bufferData) : base(bufferData)
        {
            this.Count = this.Parameters.GetValue(0, defaultValue: 1); 
        }
    }
}
