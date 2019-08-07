namespace ConsoleBuffer.Commands
{
    public abstract class Base
    {
        internal Base(string bufferData)
        {
            this.Parse(bufferData);
#if DEBUG
            this.data = bufferData;
#endif
        }

        protected abstract void Parse(string bufferData);

#if DEBUG
        private readonly string data;
        public override string ToString()
        {
            return $"{this.GetType()}: {this.data}";
        }
#endif
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
