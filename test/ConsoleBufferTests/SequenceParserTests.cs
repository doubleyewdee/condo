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
        [DataRow('\0', ControlCharacterCommand.ControlCode.NUL)]
        [DataRow('\a', ControlCharacterCommand.ControlCode.BEL)]
        [DataRow('\b', ControlCharacterCommand.ControlCode.BS)]
        [DataRow('\f', ControlCharacterCommand.ControlCode.FF)]
        [DataRow('\n', ControlCharacterCommand.ControlCode.LF)]
        [DataRow('\r', ControlCharacterCommand.ControlCode.CR)]
        [DataRow('\t', ControlCharacterCommand.ControlCode.TAB)]
        [DataRow('\v', ControlCharacterCommand.ControlCode.LF)] // lmao vertical tabs
        public void ControlCharacters(char c, ControlCharacterCommand.ControlCode code)
        {
            var parser = new SequenceParser();
            Assert.AreEqual(ParserAppendResult.Complete, parser.Append(c));
            Assert.IsInstanceOfType(parser.Command, typeof(ControlCharacterCommand));
            Assert.AreEqual(code, (parser.Command as ControlCharacterCommand).Code);
        }

        [TestMethod]
        [DataRow('^')]
        [DataRow('_')]
        public void UnsupportedAncientStuff(char c)
        {
            var ancientCommand = $"\x1b{c} here is a long string of nonsense.\0";

            var parser = new SequenceParser();
            for (var i = 0; i < ancientCommand.Length - 1; ++i)
            {
                Assert.AreEqual(ParserAppendResult.Pending, parser.Append(ancientCommand[i]));
            }
            Assert.AreEqual(ParserAppendResult.Complete, parser.Append(ancientCommand[ancientCommand.Length - 1]));
            Assert.IsInstanceOfType(parser.Command, typeof(UnsupportedCommand));
        }

        [TestMethod]
        public void OSCommands()
        {
            const string title = "this is a random title";
            var command = $"\x1b]2;{title}\a";

            var parser = new SequenceParser();
            for (var i = 0; i < command.Length - 1; ++i)
            {
                Assert.AreEqual(ParserAppendResult.Pending, parser.Append(command[i]));
            }
            Assert.AreEqual(ParserAppendResult.Complete, parser.Append(command[command.Length - 1]));
            Assert.IsInstanceOfType(parser.Command, typeof(OSCommand));

            var osCmd = parser.Command as OSCommand;
            Assert.AreEqual(title, osCmd.Title);
        }
    }
}
