namespace ConsoleBuffer
{
    using System;
    using System.Text;

    public enum ParserAppendResult
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

    // Notes on the parser:
    // - I elected to hand-roll this instead of generating a parser. This is primarily for performance purposes.
    // - The names I've chosen to given to various "areas" of parsing are made up and not based on good research on my
    //   part.  The areas themselves may be silly/erroneous. Please call me out on my bullshit as desired.
    // - The parser is not greedy. I have seen parsers which work to varying levels of greed, we will stop on the first
    //   invalid character for whatever sequence we are in (and not emit that character, nor any preceding). So for
    //   example the sequence '\e[32[33m hello' will emit an unmodified '33m hello' string as we gave up at the invalid
    //   '[' character.
    public sealed class SequenceParser
    {
        enum State
        {
            /// <summary>
            /// Indicates we are in a standard (\e) escape sequence.
            /// </summary>
            Basic = 0,
            /// <summary>
            /// Indicates we are in CSI (\e[...)
            /// </summary>
            ControlSequence,
            /// <summary>
            /// Indicates we are in an operating system (\e]) escape sequence.
            /// </summary>
            OSCommand,
            /// <summary>
            /// Indicates we are in a privacy message (\e^) sequence.
            /// </summary>
            PrivacyMessage,
            /// <summary>
            /// Indicates we are in an application program command (\e_) sequence.
            /// </summary>
            ApplicationProgramCommand,
            /// <summary>
            /// We are not in a sequence.
            /// </summary>
            None,
            /// <summary>
            /// We have completed a sequence and should reset at the next append.
            /// </summary>
            Reset,
        }

        private State state = State.None;
        private readonly StringBuilder buffer = new StringBuilder(128);

        /// <summary>
        /// The current command (may be null if none).
        /// </summary>
        public Commands.Base Command { get; private set; }

        public SequenceParser() { }

        public ParserAppendResult Append(int character)
        {
            if (this.state == State.Reset)
            {
                this.state = State.None;
                this.Command = null;
                this.buffer.Clear();
            }

            switch (this.state)
            {
            case State.None:
                return this.AppendNone(character);
            case State.Basic:
                return this.AppendBasic(character);
            case State.ControlSequence:
                return this.AppendControlSequence(character);
            case State.OSCommand:
                return this.AppendOSCommand(character);
            case State.PrivacyMessage: // these are identical for now as we do not support them.
            case State.ApplicationProgramCommand:
                return this.AppendUntilST(character);
            default:
                throw new InvalidOperationException("Invalid parser state.");
            }
        }

        private ParserAppendResult AppendNone(int character)
        {
            switch (character)
            {
            case '\0':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.NUL));
            case '\a':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.BEL));
            case '\b':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.BS));
            case '\f':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.FF));
            case '\n':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.LF));
            case '\r':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.CR));
            case '\t':
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.TAB));
            case '\v':
                // NB: old timey tech sure was funny. vertical tabs. ha ha ha.
                return this.CompleteCommand(new Commands.ControlCharacter(Commands.ControlCharacter.ControlCode.LF));
            case 0x1b: // ^[ / escape
                this.state = State.Basic;
                return ParserAppendResult.Pending;
            default:
                return ParserAppendResult.Render;
            }
        }

        private ParserAppendResult AppendBasic(int character)
        {
            switch (character)
            {
            case '[':
                this.state = State.ControlSequence;
                return ParserAppendResult.Pending;
            case ']':
                this.state = State.OSCommand;
                return ParserAppendResult.Pending;
            case '^':
                this.state = State.PrivacyMessage;
                return ParserAppendResult.Pending;
            case '_':
                this.state = State.ApplicationProgramCommand;
                return ParserAppendResult.Pending;
            default:
                return this.CompleteCommand(new Commands.Unsupported($"^[{(char)character}"), ParserAppendResult.Invalid);
            }
        }

        private ParserAppendResult AppendControlSequence(int character)
        {
            // all character values between 0x20 and 0x3f (inclusive) are considered "non-command" values and can be safely devoured prior
            // to any other value. that final value is considered the command to execute and can then parse the consumed data for validity.
            // this is probably documented somewhere although I learned it with experimentation on a couple distinct emulators...
            if (character >= 0x20 && character <= 0x3f)
            {
                this.buffer.Append((char)character);
                return ParserAppendResult.Pending;
            }

            return this.CompleteCommand(Commands.ControlSequence.Create((char)character, this.buffer.ToString()));
        }

        private ParserAppendResult AppendOSCommand(int character)
        {
            switch (character)
            {
            case '\a':
                return this.CompleteCommand(new Commands.OS(this.buffer.ToString()));
            case '\\':
                if (this.buffer.Length > 0 && this.buffer[this.buffer.Length - 1] == 0x1b)
                {
                    this.buffer.Remove(this.buffer.Length - 1, 1);
                    return this.CompleteCommand(new Commands.OS(this.buffer.ToString()));
                }
                this.buffer.Append((char)character);
                return ParserAppendResult.Pending;
            default:
                this.buffer.Append((char)character); // XXX: nukes astral plane support for giganto unicode characters. care later when emoji can be in title bars?
                return ParserAppendResult.Pending;
            }
        }

        private ParserAppendResult AppendUntilST(int character)
        {
            switch (character)
            {
            case '\\':
                if (this.buffer.Length > 0 && this.buffer[this.buffer.Length - 1] == 0x1b)
                {
                    this.buffer.Remove(this.buffer.Length - 1, 1);
                    return this.CompleteCommand(new Commands.Unsupported("(ASC or PM goo)"));
                }
                return ParserAppendResult.Pending;
            default:
                this.buffer.Append((char)character);
                return ParserAppendResult.Pending;
            }
        }

        private ParserAppendResult CompleteCommand(Commands.Base command, ParserAppendResult result = ParserAppendResult.Complete)
        {
            this.Command = command ?? throw new ArgumentNullException(nameof(command));
            this.state = State.Reset;
            return result;
        }
    }
}
