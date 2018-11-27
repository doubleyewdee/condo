namespace ConsoleBufferTests
{
    using ConsoleBuffer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class SequenceParserTests
    {
        [TestMethod]
        public void Basic()
        {
            var parser = new SequenceParser();
            foreach (var c in "hello world")
            {
                Assert.AreEqual(ParserAppendResult.Render, parser.Append(c));
            }
        }

        [TestMethod]
        [DataRow('\0', ConsoleBuffer.Commands.ControlCharacter.ControlCode.NUL)]
        [DataRow('\a', ConsoleBuffer.Commands.ControlCharacter.ControlCode.BEL)]
        [DataRow('\b', ConsoleBuffer.Commands.ControlCharacter.ControlCode.BS)]
        [DataRow('\f', ConsoleBuffer.Commands.ControlCharacter.ControlCode.FF)]
        [DataRow('\n', ConsoleBuffer.Commands.ControlCharacter.ControlCode.LF)]
        [DataRow('\r', ConsoleBuffer.Commands.ControlCharacter.ControlCode.CR)]
        [DataRow('\t', ConsoleBuffer.Commands.ControlCharacter.ControlCode.TAB)]
        [DataRow('\v', ConsoleBuffer.Commands.ControlCharacter.ControlCode.LF)] // lmao vertical tabs
        public void ControlCharacters(char c, ConsoleBuffer.Commands.ControlCharacter.ControlCode code)
        {
            var parser = new SequenceParser();
            Assert.AreEqual(ParserAppendResult.Complete, parser.Append(c));
            Assert.IsInstanceOfType(parser.Command, typeof(ConsoleBuffer.Commands.ControlCharacter));
            Assert.AreEqual(code, (parser.Command as ConsoleBuffer.Commands.ControlCharacter).Code);
        }

        [TestMethod]
        [DataRow('^')]
        [DataRow('_')]
        public void UnsupportedAncientStuff(char c)
        {
            var parser = this.EnsureCommandParses($"\x1b{c} here is a long string of nonsense.\x1b\\");
            Assert.IsInstanceOfType(parser.Command, typeof(ConsoleBuffer.Commands.Unsupported));
        }

        [TestMethod]
        [DataRow('A', ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [DataRow('B', ConsoleBuffer.Commands.CursorMove.CursorDirection.Down)]
        [DataRow('C', ConsoleBuffer.Commands.CursorMove.CursorDirection.Forward)]
        [DataRow('D', ConsoleBuffer.Commands.CursorMove.CursorDirection.Backward)]
        public void BasicCursorMovement(char modifier, ConsoleBuffer.Commands.CursorMove.CursorDirection expectedDirection)
        {
            var parser = this.EnsureCommandParses($"\x1b{modifier}");
            var cmd = parser.Command as ConsoleBuffer.Commands.CursorMove;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(expectedDirection, cmd.Direction);
            Assert.AreEqual(1, cmd.Count);
        }

        [TestMethod]
        public void OSCommands()
        {
            const string title = "this is a random title";
            var command = $"\x1b]2;{title}\a";

            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.OS;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(title, cmd.Title);
        }

        [TestMethod]
        [DataRow("", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [DataRow("0", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [DataRow("1", ConsoleBuffer.Commands.EraseIn.Parameter.After)]
        [DataRow("2", ConsoleBuffer.Commands.EraseIn.Parameter.All)]
        [DataRow("?2", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        [DataRow("-50", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        public void EraseInDisplay(string direction, ConsoleBuffer.Commands.EraseIn.Parameter expectedDirection)
        {
            var command = $"\x1b[{direction}J";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseIn;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(ConsoleBuffer.Commands.EraseIn.EraseType.Display, cmd.Type);
            Assert.AreEqual(expectedDirection, cmd.Direction);
        }

        [TestMethod]
        [DataRow("", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [DataRow("0", ConsoleBuffer.Commands.EraseIn.Parameter.Before)]
        [DataRow("1", ConsoleBuffer.Commands.EraseIn.Parameter.After)]
        [DataRow("2", ConsoleBuffer.Commands.EraseIn.Parameter.All)]
        [DataRow("?2", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        [DataRow("-50", ConsoleBuffer.Commands.EraseIn.Parameter.Unknown)]
        public void EraseInLine(string direction, ConsoleBuffer.Commands.EraseIn.Parameter expectedDirection)
        {
            var command = $"\x1b[{direction}K";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseIn;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(ConsoleBuffer.Commands.EraseIn.EraseType.Line, cmd.Type);
            Assert.AreEqual(expectedDirection, cmd.Direction);
        }

        [TestMethod]
        [DataRow("A", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [DataRow("1A", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [DataRow("867A", 867, ConsoleBuffer.Commands.CursorMove.CursorDirection.Up)]
        [DataRow("B", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Down)]
        [DataRow("C", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Forward)]
        [DataRow("D", 1, ConsoleBuffer.Commands.CursorMove.CursorDirection.Backward)]
        public void CursorMove(string code, int count, ConsoleBuffer.Commands.CursorMove.CursorDirection direction)
        {
            var command = $"\x1b[{code}";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.CursorMove;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(direction, cmd.Direction);
            Assert.AreEqual(count, cmd.Count);
        }

        [TestMethod]
        [DataRow("25", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [DataRow("?25", ConsoleBuffer.Commands.SetMode.Parameter.CursorShow)]
        [DataRow("12", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [DataRow("?12", ConsoleBuffer.Commands.SetMode.Parameter.CursorBlink)]
        [DataRow("", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        [DataRow("?", ConsoleBuffer.Commands.SetMode.Parameter.Unknown)]
        public void SetMode(string mode, ConsoleBuffer.Commands.SetMode.Parameter expectedSetting)
        {
            foreach (var cmdChar in "hl")
            {
                var on = cmdChar == 'h';
                var command = $"\x1b[{mode}{cmdChar}";
                var parser = this.EnsureCommandParses(command);
                var cmd = parser.Command as ConsoleBuffer.Commands.SetMode;
                Assert.IsNotNull(cmd);
                Assert.AreEqual(on, cmd.Set);
                Assert.AreEqual(expectedSetting, cmd.Setting);
            }
        }

        [TestMethod]
        [DataRow("", 1)]
        [DataRow("1", 1)]
        [DataRow("8675309", 1)]
        [DataRow(";;;", 1)]
        [DataRow("42", 42)]
        [DataRow("0", 1)]
        public void EraseCharacter(string data, int count)
        {
            var command = $"\x1b[{data}X";
            var parser = this.EnsureCommandParses(command);
            var cmd = parser.Command as ConsoleBuffer.Commands.EraseCharacter;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(count, cmd.Count);
        }

        [TestMethod]
        [DataRow("G", 0, -1)]
        [DataRow("1G", 0, -1)]
        [DataRow("42G", 41, -1)]
        [DataRow("d", -1, 0)]
        [DataRow("1d", -1, 0)]
        [DataRow("42d", -1, 41)]
        [DataRow("H", 0, 0)]
        [DataRow("1;1H", 0, 0)]
        [DataRow("42H", 0, 41)]
        [DataRow(";42H", 41, 0)]
        public void SetCursorPosition(string data, int expectedX, int expectedY)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetCursorPosition;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(expectedX, cmd.PosX);
            Assert.AreEqual(expectedY, cmd.PosY);
        }

        [TestMethod]
        [DataRow("-1")]
        public void SGRInvalid(string data)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.IsFalse(cmd.HaveForeground);
            Assert.AreEqual(new Character.ColorInfo(), cmd.ForegroundColor);
            Assert.IsFalse(cmd.HaveBackground);
            Assert.AreEqual(new Character.ColorInfo(), cmd.BackgroundColor);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.ForegroundBright);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.BackgroundBright);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.Underline);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None, cmd.Inverse);

            Assert.IsFalse(cmd.HaveBasicForeground);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None, cmd.BasicForegroundColor);
            Assert.IsFalse(cmd.HaveBasicBackground);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.None, cmd.BasicBackgroundColor);
        }

        [TestMethod]
        [DataRow("0")]
        [DataRow("")]
        public void SGRReset(string data)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.IsFalse(cmd.HaveForeground);
            Assert.AreEqual(new Character.ColorInfo(), cmd.ForegroundColor);
            Assert.IsFalse(cmd.HaveBackground);
            Assert.AreEqual(new Character.ColorInfo(), cmd.BackgroundColor);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.ForegroundBright);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.BackgroundBright);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.Underline);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset, cmd.Inverse);

            Assert.IsTrue(cmd.HaveBasicForeground);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, cmd.BasicForegroundColor);
            Assert.IsTrue(cmd.HaveBasicBackground);
            Assert.AreEqual(ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, cmd.BasicBackgroundColor);
        }

        [TestMethod]
        [DataRow("1", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [DataRow("2", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        [DataRow("22", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRBold(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(expectedValue, cmd.ForegroundBright);
        }

        [TestMethod]
        [DataRow("4", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [DataRow("24", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRUnderline(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(expectedValue, cmd.Underline);
        }

        [TestMethod]
        [DataRow("7", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set)]
        [DataRow("27", ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Unset)]
        public void SGRInverse(string data, ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue expectedValue)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.AreEqual(expectedValue, cmd.Inverse);
        }

        [TestMethod]
        [DataRow("30", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, false)]
        [DataRow("31", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, false)]
        [DataRow("32", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, false)]
        [DataRow("33", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, false)]
        [DataRow("34", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, false)]
        [DataRow("35", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, false)]
        [DataRow("36", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, false)]
        [DataRow("37", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, false)]
        [DataRow("90", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, true)]
        [DataRow("91", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, true)]
        [DataRow("92", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, true)]
        [DataRow("93", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, true)]
        [DataRow("94", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, true)]
        [DataRow("95", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, true)]
        [DataRow("96", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, true)]
        [DataRow("97", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, true)]
        public void SGRBasicForegroundColors(string data, ConsoleBuffer.Commands.SetGraphicsRendition.Colors expectedColor, bool expectedBright)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.IsTrue(cmd.HaveBasicForeground);
            Assert.AreEqual(expectedColor, cmd.BasicForegroundColor);
            Assert.AreEqual(expectedBright ? ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set :
                                             ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None,
                            cmd.ForegroundBright);
        }

        [TestMethod]
        [DataRow("40", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, false)]
        [DataRow("41", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, false)]
        [DataRow("42", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, false)]
        [DataRow("43", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, false)]
        [DataRow("44", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, false)]
        [DataRow("45", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, false)]
        [DataRow("46", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, false)]
        [DataRow("47", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, false)]
        [DataRow("100", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Black, true)]
        [DataRow("101", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Red, true)]
        [DataRow("102", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Green, true)]
        [DataRow("103", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Yellow, true)]
        [DataRow("104", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Blue, true)]
        [DataRow("105", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Magenta, true)]
        [DataRow("106", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.Cyan, true)]
        [DataRow("107", ConsoleBuffer.Commands.SetGraphicsRendition.Colors.White, true)]
        public void SGRBasicBackgroundColors(string data, ConsoleBuffer.Commands.SetGraphicsRendition.Colors expectedColor, bool expectedBright)
        {
            var parser = this.EnsureCommandParses($"\x1b[{data}m");
            var cmd = parser.Command as ConsoleBuffer.Commands.SetGraphicsRendition;
            Assert.IsNotNull(cmd);
            Assert.IsTrue(cmd.HaveBasicBackground);
            Assert.AreEqual(expectedColor, cmd.BasicBackgroundColor);
            Assert.AreEqual(expectedBright ? ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.Set :
                                             ConsoleBuffer.Commands.SetGraphicsRendition.FlagValue.None,
                            cmd.BackgroundBright);
        }

        private SequenceParser EnsureCommandParses(string command)
        {
            var parser = new SequenceParser();
            for (var i = 0; i < command.Length - 1; ++i)
            {
                Assert.AreEqual(ParserAppendResult.Pending, parser.Append(command[i]));
            }
            Assert.AreEqual(ParserAppendResult.Complete, parser.Append(command[command.Length - 1]));

            return parser;
        }
    }
}
