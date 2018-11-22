namespace ConsoleBuffer.Commands
{
    public sealed class EraseInDisplay : ControlSequence
    {
        public EraseInDisplay(string bufferData) : base(bufferData) { }
    }
}
