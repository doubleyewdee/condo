namespace ConsoleBuffer
{
    public sealed class EraseInDisplayCommand : ControlSequenceCommand
    {
        public EraseInDisplayCommand(string bufferData) : base(bufferData) { }
    }
}
