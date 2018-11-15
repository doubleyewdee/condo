namespace condo
{
    using ConsoleBuffer;
    using System.Text;
    using System.Windows.Input;

    // We take a two input approach to handling. For lots of stuff we can use the 'TextInput' event with the rather awfully named three children
    // of TextCompositionEventArgs:
    // - Text: good 'ol regular text.
    // - SystemText: This is what happens when you hold down alt. System? idk.
    // - ControlText: This is what happens when you hold down ctrl.
    // this event is awesome for not having to care about the state of the keyboard for... most stuff.
    // For function keys (and maybe some others)? we'll trap the KeyDown events instead and inspect the keyboard to pass on the appropriate values.
    sealed class KeyHandler
    {
        private readonly ConsoleWrapper console;

        public KeyHandler(ConsoleWrapper console)
        {
            this.console = console;
        }

        public void OnTextInput(object sender, TextCompositionEventArgs e)
        {
            var ctrl = false;
            var alt = false;
            string text = e.Text;
            if (e.ControlText.Length > 0)
            {
                ctrl = true;
                text = e.ControlText;
            }
            else if (e.SystemText.Length > 0)
            {
                alt = true;
                text = e.SystemText;
            }

            this.console.SendText(Encoding.UTF8.GetBytes(text), alt, ctrl);
            e.Handled = true;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            // XXX: codeme.
        }
    }
}
