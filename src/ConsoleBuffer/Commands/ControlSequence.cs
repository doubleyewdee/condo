namespace ConsoleBuffer.Commands
{
    using System;
    using System.Collections.Generic;

    public abstract class ControlSequence : Base
    {
        public static Base Create(char command, string bufferData)
        {
            switch (command)
            {
            case 'h':
            case 'l':
                return new SetMode(bufferData, command == 'h');
            case 'H':
                return new EraseCharacter(bufferData);
            case 'J':
                return new EraseIn(bufferData, EraseIn.EraseType.Display);
            case 'K':
                return new EraseIn(bufferData, EraseIn.EraseType.Line);
            }
            return new Unsupported($"^[[{bufferData}{command}");
        }

        public bool IsExtended { get; private set; }
        protected IList<string> Parameters { get; private set; }
        protected ControlSequence(string bufferData) : base(bufferData) { }
        protected override void Parse(string bufferData)
        {
            if (bufferData.Length == 0)
            {
                this.Parameters = Array.Empty<string>();
                return;
            }

            var startIndex = 0;
            if (bufferData[0] == '?')
            {
                this.IsExtended = true;
                startIndex = 1;
            }

            this.Parameters = bufferData.Substring(startIndex).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
