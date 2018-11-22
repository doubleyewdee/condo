namespace ConsoleBuffer.Commands
{
    public abstract class Base
    {
        internal Base(string bufferData)
        {
            this.Parse(bufferData);
        }

        protected abstract void Parse(string bufferData);
    }

    public sealed class Unsupported : Base
    {
        internal Unsupported(string buffer) : base(null)
        {
            Logger.Verbose($"Unsupported command: {buffer}");
        }
        protected override void Parse(string bufferData) { }
    }
}
