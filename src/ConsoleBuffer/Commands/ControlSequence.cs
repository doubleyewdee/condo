namespace ConsoleBuffer.Commands
{
    using System;
    using System.Collections.Generic;

    public abstract class ControlSequence : Base
    {
        public static Base Create(char command, string bufferData)
        {
            //Trace.WriteLine($"Parsing sequence ^[[{bufferData}{command}");
            switch (command)
            {
            case 'A':
            case 'B':
            case 'C':
            case 'D':
                return new CursorMove(bufferData, command);
            case 'G':
            case 'd':
            case 'H':
            case 'f':
                return new SetCursorPosition(bufferData, command);
            case 'h':
            case 'l':
                return new SetMode(bufferData, command == 'h');
            case 'J':
                return new EraseIn(bufferData, EraseIn.EraseType.Display);
            case 'K':
                return new EraseIn(bufferData, EraseIn.EraseType.Line);
            case 'm':
                return new SetGraphicsRendition(bufferData);
            case 'X':
                return new EraseCharacter(bufferData);
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

            this.Parameters = bufferData.Substring(startIndex).Split(new[] { ';' });
        }

        protected int ParameterToNumber(int offset, int defaultValue = 0, int maxValue = short.MaxValue)
        {
            if (this.Parameters.Count > offset && ushort.TryParse(this.Parameters[offset], out var paramValue))
            {
                return Math.Max(defaultValue, Math.Min(paramValue, maxValue));
            }

            return defaultValue;
        }
    }
}
