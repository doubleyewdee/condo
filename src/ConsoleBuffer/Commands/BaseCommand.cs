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
        internal UnsupportedCommand(string buffer) : base(null)
        {
            Logger.Verbose($"Unsupported command: {buffer}");
        }
        protected override void Parse(string bufferData) { }
    }
}
