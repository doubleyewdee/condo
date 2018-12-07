namespace ConsoleBuffer.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public abstract class ControlSequence : Base
    {
        public class ParsedParameters
        {
            public int GetValue(int offset, int defaultValue = 0, int maxValue = int.MaxValue)
            {
                if(offset < this.Count)
                {
                    return Math.Max(defaultValue, Math.Min(this.Parameters[offset], maxValue));
                }

                return defaultValue;
            }

            public int Count { get; set; }
            public int[] Parameters { get; set; } = new int[16];
        }

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
        protected ParsedParameters Parameters { get; private set; } = new ParsedParameters();
        protected ControlSequence(string bufferData) : base(bufferData) { }

        protected override void Parse(string bufferData)
        {
            this.Parameters.Count = 0;

            if (bufferData.Length == 0)
            {
                this.Parameters.Parameters[0] = 0;
                return;
            }

            var startIndex = 0;
            if (bufferData[0] == '?')
            {
                this.IsExtended = true;
                startIndex = 1;
            }

            var idx = startIndex;
            var len = bufferData.Length;

            // Parses the string in bufferData to extract the int's represented by text,
            // it will do it without an allocations.  bufferData takes the form of
            // NUM;NUM;NUM... for an unknown number of of NUM's
            while (idx < len)
            {
                var val = 0;
                while (idx < len && bufferData[idx] != ';')
                {
                    val = val * 10 + (bufferData[idx++] - '0');
                }
                idx++;
                this.Parameters.Parameters[this.Parameters.Count++] = (ushort)val;
            }
        }
    }
}
