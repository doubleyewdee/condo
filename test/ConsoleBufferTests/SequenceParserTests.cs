namespace ConsoleBufferTests
{
    using System;
    using ConsoleBuffer;
    using Xunit;

    public sealed class SequenceParserTests
    {
        [Fact]
        public void Basic()
        {
            var parser = new SequenceParser();
            foreach (var c in "hello world")
            {
                Assert.Equal(ParserAppendResult.Render, parser.Append(c));
            }
        }

        [Theory]
        [InlineData("f")]
        [InlineData("ba")]
        [InlineData("12g32")]
        [InlineData("-y")]
        [InlineData("-3a")]
        [InlineData("3a-")]
        [InlineData("3;f")]
        [InlineData("f;3")]
        public void BadCharacters(string data)
        {
            var command = $"\x1b[{data}X";

            Assert.ThrowsAny<Exception>(() => { this.EnsureCommandParses(command); });
            
        }

        [Theory]
        [InlineData("1;2;3;4;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23;24;25;26;27;28;29;30;31;32;33;34")]
        public void BadParameterList(string data)
        {
            var command = $"\x1b[{data}X";

            Assert.Throws<IndexOutOfRangeException>(() => { this.EnsureCommandParses(command); });
        }

        [Theory]
        [InlineData('\0', ConsoleBuffer.Commands.ControlCharacter.ControlCode.NUL)]
        [InlineData('\a', ConsoleBuffer.Commands.ControlCharacter.ControlCode.BEL)]
        [InlineData('\b', ConsoleBuffer.Commands.ControlCharacter.ControlCode.BS)]
        [InlineData('\f', ConsoleBuffer.Commands.ControlCharacter.ControlCode.FF)]
        [InlineData('\n', ConsoleBuffer.Commands.ControlCharacter.ControlCode.LF)]
        [InlineData('\r', ConsoleBuffer.Commands.ControlCharacter.ControlCode.CR)]
        [InlineData('\t', ConsoleBuffer.Commands.ControlCharacter.ControlCode.TAB)]
        [InlineData('\v', ConsoleBuffer.Commands.ControlCharacter.ControlCode.LF)] // lmao vertical tabs
        public void ControlCharacters(char c, ConsoleBuffer.Commands.ControlCharacter.ControlCode code)
        {
            var parser = new SequenceParser();
            Assert.Equal(ParserAppendResult.Complete, parser.Append(c));
            Assert.IsType<ConsoleBuffer.Commands.ControlCharacter>(parser.Command);
            Assert.Equal(code, (parser.Command as ConsoleBuffer.Commands.ControlCharacter).Code);
        }

        [Theory]
        [InlineData('^')]
        [InlineData('_')]
        public void UnsupportedAncientStuff(char c)
        {
            var parser = this.EnsureCommandParses($"\x1b{c} here is a long string of nonsense.\x1b\\");
            Assert.IsType<ConsoleBuffer.Commands.Unsupported>(parser.Command);
        }

        [Theory]
        [InlineData('A', ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [InlineData('B', ConsoleBuffer.Commands.CursorMove.CursorDirection.Down)]
        [InlineData('C', ConsoleBuffer.Commands.CursorMove.CursorDirection.Forward)]
        [InlineData('D', ConsoleBuffer.Commands.CursorMove.CursorDirection.Backward)]
        public void BasicCursorMovement(char modifier, ConsoleBuffer.Commands.CursorMove.CursorDirection expectedDirection)
        {
            var parser = this.EnsureCommandParses($"\x1b{modifier}");
            var cmd = parser.Command as ConsoleBuffer.Commands.CursorMove;
            Assert.NotNull(cmd);
            Assert.Equal(expectedDirection, cmd.Direction);
            Assert.Equal(1, cmd.Count);
        }

        [Fact]
        public void OSCommands()
        {
            const string title = "this is a random title";
            var command = $"\x1b]2;{title}\a";

            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.OS;
            Assert.NotNull(cmd);
            Assert.Equal(title, cmd.Title);
        }

        [Theory]
        [InlineData("", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [InlineData("0", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [InlineData("1", ConsoleBuffer.Commands.EraseIn.Parameter.After)]
        [InlineData("2", ConsoleBuffer.Commands.EraseIn.Parameter.All)]
        [InlineData("?2", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        [InlineData("-50", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        public void EraseInDisplay(string direction, ConsoleBuffer.Commands.EraseIn.Parameter expectedDirection)
        {
            var command = $"\x1b[{direction}J";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseIn;
            Assert.NotNull(cmd);
            Assert.Equal(ConsoleBuffer.Commands.EraseIn.EraseType.Display, cmd.Type);
            Assert.Equal(expectedDirection, cmd.Direction);
        }

        [Theory]
        [InlineData("", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [InlineData("0", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [InlineData("1", ConsoleBuffer.Commands.EraseIn.Parameter.After)]
        [InlineData("2", ConsoleBuffer.Commands.EraseIn.Parameter.All)]
        [InlineData("?2", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        [InlineData("-50", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        public void EraseInLine(string direction, ConsoleBuffer.Commands.EraseIn.Parameter expectedDirection)
        {
            var command = $"\x1b[{direction}K";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseIn;
            Assert.NotNull(cmd);
            Assert.Equal(ConsoleBuffer.Commands.EraseIn.EraseType.Line, cmd.Type);
            Assert.Equal(expectedDirection, cmd.Direction);
        }

        [Theory]
        [InlineData("A", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [InlineData("1A", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [InlineData("867A", 867, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [InlineData("B", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Down)]
        [InlineData("C", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Forward)]
        [InlineData("D", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Backward)]
        public void CursorMove(string code, int count, ConsoleBuffer.Commands.CursorMove.CursorDirection direction)
        {
            var command = $"\x1b[{code}";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.CursorMove;
            Assert.NotNull(cmd);
            Assert.Equal(direction, cmd.Direction);
            Assert.Equal(count, cmd.Count);
        }

        [Theory]
        [InlineData("25", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [InlineData("?25", ConsoleBuffer.Commands.SetMode.Parameter.CursorShow)]
        [InlineData("12", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [InlineData("?12", ConsoleBuffer.Commands.SetMode.Parameter.CursorBlink)]
        [InlineData("", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [InlineData("?", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        public void SetMode(string mode, ConsoleBuffer.Commands.SetMode.Parameter expectedSetting)
        {
            foreach (var cmdChar in "hl")
            {
                var on = cmdChar == 'h';
                var command = $"\x1b[{mode}{cmdChar}";
                var parser = this.EnsureCommandParses(command);
                var cmd = parser.Command as ConsoleBuffer.Commands.SetMode;
                Assert.NotNull(cmd);
                Assert.Equal(on, cmd.Set);
                Assert.Equal(expectedSetting, cmd.Setting);
            }
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData("1", 1)]
        [InlineData("8675309", 1)]
        [InlineData(";;;", 1)]
        [InlineData("42", 42)]
        [InlineData("0", 1)]
        public void EraseCharacter(string data, int count)
        {
            var command = $"\x1b[{data}X";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseCharacter;
            Assert.NotNull(cmd);
            Assert.Equal(count, cmd.Count);
        }

        [Theory]
        [InlineData("G", 0, -1)]
        [InlineData("1G", 0, -1)]
        [InlineData("42G", 41, -1)]
        [InlineData("d", -1, 0)]
        [InlineData("1d", -1, 0)]
        [InlineData("42d", -1, 41)]
        [InlineData("H", 0, 0)]
        [InlineData("1;1H", 0, 0)]
        [InlineData("42H", 0, 41)]
        [InlineData(";42H", 41, 0)]
        public void SetCursorPosition(string data, int expectedX, int expectedY)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetCursorPosition;
            Assert.NotNull(cmd);
            Assert.Equal(expectedX, cmd.PosX);
            Assert.Equal(expectedY, cmd.PosY);
        }

        [Theory]
        [InlineData("-1")]
        public void SGRInvalid(string data)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.False(cmd.HaveForeground);
            Assert.Equal(new Character.ColorInfo(), cmd.ForegroundColor);
            Assert.False(cmd.HaveBackground);
            Assert.Equal(new Character.ColorInfo(), cmd.BackgroundColor);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.ForegroundBright);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.BackgroundBright);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.Underline);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.Inverse);

            Assert.False(cmd.HaveBasicForeground);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None, cmd.BasicForegroundColor);
            Assert.False(cmd.HaveBasicBackground);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None, cmd.BasicBackgroundColor);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("")]
        public void SGRReset(string data)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.False(cmd.HaveForeground);
            Assert.Equal(new Character.ColorInfo(), cmd.ForegroundColor);
            Assert.False(cmd.HaveBackground);
            Assert.Equal(new Character.ColorInfo(), cmd.BackgroundColor);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.ForegroundBright);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.BackgroundBright);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.Underline);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.Inverse);

            Assert.True(cmd.HaveBasicForeground);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, cmd.BasicForegroundColor);
            Assert.True(cmd.HaveBasicBackground);
            Assert.Equal(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, cmd.BasicBackgroundColor);
        }

        [Theory]
        [InlineData("1", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [InlineData("22", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRBold(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(expectedValue, cmd.ForegroundBright);
        }

        [Theory]
        [InlineData("4", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [InlineData("24", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRUnderline(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(expectedValue, cmd.Underline);
        }

        [Theory]
        [InlineData("7", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [InlineData("27", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRInverse(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(expectedValue, cmd.Inverse);
        }

        [Theory]
        [InlineData("30", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, false)]
        [InlineData("31", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, false)]
        [InlineData("32", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, false)]
        [InlineData("33", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, false)]
        [InlineData("34", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, false)]
        [InlineData("35", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, false)]
        [InlineData("36", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, false)]
        [InlineData("37", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, false)]
        [InlineData("90", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, true)]
        [InlineData("91", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, true)]
        [InlineData("92", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, true)]
        [InlineData("93", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, true)]
        [InlineData("94", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, true)]
        [InlineData("95", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, true)]
        [InlineData("96", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, true)]
        [InlineData("97", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, true)]
        public void SGRBasicForegroundColors(string data, ConsoleBuffer.Commands.SetGraphicsRendition.Colors expectedColor, bool expectedBright)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.True(cmd.HaveBasicForeground);
            Assert.Equal(expectedColor, cmd.BasicForegroundColor);
            Assert.Equal(expectedBright ? ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set :
                                             ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None,
                            cmd.ForegroundBright);
        }

        [Theory]
        [InlineData("40", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, false)]
        [InlineData("41", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, false)]
        [InlineData("42", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, false)]
        [InlineData("43", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, false)]
        [InlineData("44", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, false)]
        [InlineData("45", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, false)]
        [InlineData("46", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, false)]
        [InlineData("47", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, false)]
        [InlineData("100", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, true)]
        [InlineData("101", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, true)]
        [InlineData("102", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, true)]
        [InlineData("103", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, true)]
        [InlineData("104", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, true)]
        [InlineData("105", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, true)]
        [InlineData("106", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, true)]
        [InlineData("107", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, true)]
        public void SGRBasicBackgroundColors(string data, ConsoleBuffer.Commands.SetGraphicsRendition.Colors expectedColor, bool expectedBright)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.True(cmd.HaveBasicBackground);
            Assert.Equal(expectedColor, cmd.BasicBackgroundColor);
            Assert.Equal(expectedBright ? ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set :
                                             ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None,
                            cmd.BackgroundBright);
        }

        [Theory]
        [InlineData("38;5", false, 0)]
        [InlineData("38;5;", false, 0)]
        [InlineData("38;5;-1", false, 0)]
        [InlineData("38;5;0", true, 0)]
        [InlineData("38;5;255", true, 255)]
        [InlineData("38;5;256", false, 0)]
        public void SGRXtermForegroundIndex(string data, bool haveXtermColor, int expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(haveXtermColor, cmd.HaveXtermForeground);
            if (haveXtermColor)
            {
                Assert.False(cmd.HaveBasicForeground);
                Assert.False(cmd.HaveForeground);
                Assert.True(cmd.HaveXtermForeground);
                Assert.Equal(expectedValue, cmd.XtermForegroundColor);
            }
        }

        [Theory]
        [InlineData("48;5", false, 0)]
        [InlineData("48;5;", false, 0)]
        [InlineData("48;5;-1", false, 0)]
        [InlineData("48;5;0", true, 0)]
        [InlineData("48;5;255", true, 255)]
        [InlineData("48;5;256", false, 0)]
        public void SGRXtermBackgroundIndex(string data, bool haveXtermColor, int expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(haveXtermColor, cmd.HaveXtermBackground);
            if (haveXtermColor)
            {
                Assert.False(cmd.HaveBasicBackground);
                Assert.False(cmd.HaveBackground);
                Assert.True(cmd.HaveXtermBackground);
                Assert.Equal(expectedValue, cmd.XtermBackgroundColor);
            }
        }

        [Theory]
        [InlineData("38;2", false, 0, 0, 0)]
        [InlineData("38;2;", false, 0, 0, 0)]
        [InlineData("38;2;1", false, 0, 0, 0)]
        [InlineData("38;2;1;2", false, 0, 0, 0)]
        [InlineData("38;2;1;2;", false, 0, 0, 0)]
        [InlineData("38;2;1;2;3", true, 1, 2, 3)]
        [InlineData("38;2;0;0;0", true, 0, 0, 0)]
        [InlineData("38;2;255;255;255", true, 255, 255, 255)]
        public void SGRXtermForegroundRGB(string data, bool haveColor, int r, int g, int b)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(haveColor, cmd.HaveForeground);
            if (haveColor)
            {
                Assert.False(cmd.HaveBasicForeground);
                Assert.False(cmd.HaveXtermForeground);
                Assert.True(cmd.HaveForeground);
                Assert.Equal(new Character.ColorInfo { R = (byte)r, G = (byte)g, B = (byte)b }, cmd.ForegroundColor);
            }
        }

        [Theory]
        [InlineData("48;2", false, 0, 0, 0)]
        [InlineData("48;2;", false, 0, 0, 0)]
        [InlineData("48;2;1", false, 0, 0, 0)]
        [InlineData("48;2;1;2", false, 0, 0, 0)]
        [InlineData("48;2;1;2;", false, 0, 0, 0)]
        [InlineData("48;2;1;2;3", true, 1, 2, 3)]
        [InlineData("48;2;0;0;0", true, 0, 0, 0)]
        [InlineData("48;2;255;255;255", true, 255, 255, 255)]
        public void SGRXtermBackgroundRGB(string data, bool haveColor, int r, int g, int b)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.NotNull(cmd);
            Assert.Equal(haveColor, cmd.HaveBackground);
            if (haveColor)
            {
                Assert.False(cmd.HaveBasicBackground);
                Assert.False(cmd.HaveXtermBackground);
                Assert.True(cmd.HaveBackground);
                Assert.Equal(new Character.ColorInfo { R = (byte)r, G = (byte)g, B = (byte)b }, cmd.BackgroundColor);
            }
        }

        private SequenceParser EnsureCommandParses(string command)
        {
            var parser = new SequenceParser();
            for (var i = 0; i < command.Length - 1; ++i)
            {
                Assert.Equal(ParserAppendResult.Pending, parser.Append(command[i]));
            }

            Assert.Equal(ParserAppendResult.Complete, parser.Append(command[command.Length - 1]));

            return parser;
        }
    }
}
