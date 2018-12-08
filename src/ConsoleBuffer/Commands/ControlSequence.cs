namespace ConsoleBuffer.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public abstract class ControlSequence : Base
    {
        public sealed class ParsedParameters
        {
            // This 'should' be more than enough slots for parameters,
            // if we get more than this, it will be considered bad data
            // and throw an exception.
            private static readonly int maxParameters = 32;
            public static readonly int errorValue = -1; // eventually gets converted to unsigned, so this can be an error

            public int GetValue(int offset, int defaultValue = -1, int maxValue = int.MaxValue)
            {
                if(offset < this.Count)
                {
                    var num = this.Parameters[offset];

                    // If we had a parse error, then return the default
                    if (num == errorValue)
                    {
                        return defaultValue;
                    }

                    return Math.Max(defaultValue, Math.Min(num, maxValue));
                }

                return defaultValue;
            }

            public void Clear()
            {
                this.Count = 0;
                for (int idx = 0; idx < maxParameters; idx++)
                {
                    this.Parameters[idx] = 0;
                }
            }

            public int Count { get; set; }
            public int[] Parameters { get; set; } = new int[maxParameters];
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
            // Get our parameter list to known state
            this.Parameters.Clear();

            // If the buffer is empty, nothing to do here
            if (bufferData.Length == 0)
            {
                return;
            }

            // Is this an extened command, if so, flag and move past it
            var startIndex = 0;
            if (bufferData[0] == '?')
            {
                this.IsExtended = true;
                startIndex = 1;
            }

            // Down to business
            var idx = startIndex;
            var len = bufferData.Length;

            // Parses the string in bufferData to extract the int's represented by text,
            // it will do it without an allocations.  bufferData takes the form of
            // NUM;NUM;NUM... for an unknown number of of NUM's
            while (idx < len)
            {
                // Parse the number
                var val = 0;
                bool neg = false;
                while (idx < len && bufferData[idx] != ';')
                {
                    if (bufferData[idx] == '-')
                    {
                        neg = true;
                        idx++;
                    }

                    char c = bufferData[idx];

                    // Check for bogus characters
                    if ((c < '0' || c > '9') && c != ';')
                    {
                        throw new ArgumentException($"Bad control sequence: {bufferData}");
                    }

                    // Take the previous character value (or accumulated value), multiply by 10
                    // add in the current characteres numerical value.
                    val = val * 10 + (c - '0');
                    idx++;  // next char

                }

                idx++; // get past the ';'

                // Check for ushort overflow
                if (val > ushort.MaxValue || neg == true)
                {
                    val = ParsedParameters.errorValue;
                }

                this.Parameters.Parameters[this.Parameters.Count++] = val;
            }
        }
    }
}
