namespace ConsoleBuffer.Commands
{
    public sealed class ControlCharacter : Base
    {
        public enum ControlCode
        {
            /// <summary>
            /// Not really a command but a notable character we may wish to specially handle (\0 or '^ ')
            /// </summary>
            NUL = 0,
            /// <summary>
            /// Beep beep (\a or ^G)
            /// </summary>
            BEL,
            /// <summary>
            /// Backspace (\b or ^H)
            /// </summary>
            BS,
            /// <summary>
            /// Carriage return (\r or ^M)
            /// </summary>
            CR,
            /// <summary>
            /// Form feed (\f or ^L)
            /// </summary>
            FF,
            /// <summary>
            /// Line-feed (\n or ^J)
            /// </summary>
            LF,
            /// <summary>
            /// Horizontal tab (\t or ^I)
            /// </summary>
            TAB,
        }

        public readonly ControlCode Code;

        public ControlCharacter(ControlCode code) : base(null)
        {
            this.Code = code;
        }

        protected override void Parse(string bufferData) { }
    }
}
