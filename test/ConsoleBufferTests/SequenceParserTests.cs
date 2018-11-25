namespace ConsoleBufferTests
{
    using ConsoleBuffer;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SequenceParserTests
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
