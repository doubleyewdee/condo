namespace ConsoleBuffer
{
    public abstract class BaseCommand
    {
        internal BaseCommand(string bufferData)
        {
            this.Parse(bufferData);
        }

        protected abstract void Parse(string bufferData);
    }

    public sealed class UnsupportedCommand : BaseCommand
    {
        public UnsupportedCommand() : base(null) { }
        protected override void Parse(string bufferData) { }
    }
}
