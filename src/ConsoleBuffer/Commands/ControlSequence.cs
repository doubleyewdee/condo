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
            case 'J':
                var cmd = new EraseInDisplay(bufferData);
                if (!cmd.IsExtended) // currently no support for selective erase in display
                    return cmd;
                break;
            }
            return new Unsupported($"^[[{bufferData}{command}");
        }

        protected bool IsExtended { get; private set; }
        protected IList<string> Parameters { get; private set; }
        protected ControlSequence(string bufferData) : base(bufferData) { }
        protected override void Parse(string bufferData)
        {
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
