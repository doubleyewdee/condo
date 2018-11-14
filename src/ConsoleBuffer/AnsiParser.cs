namespace ConsoleBuffer
{
    enum ParserAppendResult
    {
        /// <summary>
        /// Sequence incomplete.
        /// </summary>
        Pending = 0,
        /// <summary>
        /// Sequence complete, parser is ready to query for command.
        /// </summary>
        Complete,
        /// <summary>
        /// Sequence terminated due to invalid/unexpected terminator.
        /// </summary>
        Invalid,
        /// <summary>
        /// Character is not part of a parsing sequence and may be rendered.
        /// </summary>
        Render,
        /// <summary>
        /// Invalid parser state, should not occur.
        /// </summary>
        None,
    }

    enum ParserCommand
    {
        /// <summary>
        /// Line-feed (\n)
        /// </summary>
        LF = 0,
        /// <summary>
        /// Carriage return (\r)
        /// </summary>
        CR,
    }

    sealed class AnsiParser
    {
        enum SequenceType
        {
            /// <summary>
            /// Indicates we are in a standard (\033) escape sequence.
            /// </summary>
            Standard = 0,
            /// <summary>
            /// Indicates we are in an operating system (\033 ]) escape sequence.
            /// </summary>
            OSCommand,
            /// <summary>
            /// We are not in a sequence.
            /// </summary>
            None,
        }
        private SequenceType sequenceType = SequenceType.None;
        private bool inSequence = false;

        public ParserCommand CurrentCommand { get; private set; }

        public AnsiParser()
        {
        }

        public ParserAppendResult Append(int character)
        {
            switch (this.sequenceType)
            {
            case SequenceType.None:
                switch (character)
                {
                case '\n':
                    this.CurrentCommand = ParserCommand.LF;
                    return ParserAppendResult.Complete;
                case '\r':
                    this.CurrentCommand = ParserCommand.CR;
                    return ParserAppendResult.Complete;
                default:
                    return ParserAppendResult.Render;
                }
            }

            return ParserAppendResult.None; // should be unreachable.
        }
    }
}