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

    // Notes on the parser:
    // - I elected to hand-roll this instead of generating a parser. This is primarily for performance purposes.
    // - The names I've chosen to given to various "areas" of parsing are made up and not based on good research on my
    //   part.  The areas themselves may be silly/erroneous. Please call me out on my bullshit as desired.
    // - The parser is not greedy. I have seen parsers which work to varying levels of greed, we will stop on the first
    //   invalid character for whatever sequence we are in (and not emit that character, nor any preceding). So for
    //   example the sequence '\e[32[33m hello' will emit an unmodified '33m hello' string as we gave up at the invalid
    //   '[' character.
    sealed class SequenceParser
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

        public SequenceParser()
        {
        }

        public ParserAppendResult Append(int character)
        {
            switch (this.sequenceType)
            {
            case SequenceType.None:
                switch (character)
                {
                case '\0':
                    this.CurrentCommand = ParserCommand.NUL;
                    return ParserAppendResult.Complete;
                case '\a':
                    this.CurrentCommand = ParserCommand.BEL;
                    return ParserAppendResult.Complete;
                case '\b':
                    this.CurrentCommand = ParserCommand.BS;
                    return ParserAppendResult.Complete;
                case '\f':
                    this.CurrentCommand = ParserCommand.FF;
                    return ParserAppendResult.Complete;
                case '\n':
                    this.CurrentCommand = ParserCommand.LF;
                    return ParserAppendResult.Complete;
                case '\r':
                    this.CurrentCommand = ParserCommand.CR;
                    return ParserAppendResult.Complete;
                case '\t':
                    this.CurrentCommand = ParserCommand.TAB;
                    return ParserAppendResult.Complete;
                case '\v':
                    this.CurrentCommand = ParserCommand.LF; // XXX: lazily treat these as same
                    return ParserAppendResult.Complete;
                default:
                    return ParserAppendResult.Render;
                }
            }

            return ParserAppendResult.None; // should be unreachable.
        }
    }
}