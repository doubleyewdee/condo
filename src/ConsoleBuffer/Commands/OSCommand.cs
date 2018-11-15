namespace ConsoleBuffer
{
    public sealed class OSCommand : BaseCommand
    {
        public enum Type
        {
            SetTitle,
            Unsupported,
        }

        public Type Command { get; private set; }
        public string Title { get; private set; }

        public OSCommand(string bufferData) : base(bufferData) { }

        protected override void Parse(string bufferData)
        {
            this.Command = Type.Unsupported;
            var firstSemicolon = bufferData.IndexOf(';');
            if (firstSemicolon > 0)
            {
                var cmdStr = bufferData.Substring(0, firstSemicolon);
                var dataStr = bufferData.Substring(firstSemicolon + 1);
                switch (cmdStr)
                {
                case "0": // we just shove these together for now.
                case "2":
                    this.Command = Type.SetTitle;
                    this.Title = dataStr;
                    break;
                default:
                    return;
                }
            }
        }
    }
}