namespace condo
{
    using ConsoleBuffer;
    using System;
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
        private bool IsCtrlModified { get => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl); }

        public event EventHandler<KeyboardShortcutEventArgs> KeyboardShortcut;

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
            // XXX: need to figure out how to handle DECKP*M rebinds. too lazy atm.
            // XXX2: might need to pass alt state for various function/etc keys?
            e.Handled = true;
            switch (e.Key)
            {
            case Key.Up:
                if (this.IsCtrlModified)
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[1;5A"), false, true);
                }
                else
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[A"), false, false);
                }
                break;
            case Key.Down:
                if (this.IsCtrlModified)
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[1;5B"), false, true);
                }
                else
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[B"), false, false);
                }
                break;
            case Key.Right:
                if (this.IsCtrlModified)
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[1;5C"), false, true);
                }
                else
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[C"), false, false);
                }
                break;
            case Key.Left:
                if (this.IsCtrlModified)
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[1;5D"), false, true);
                }
                else
                {
                    this.console.SendText(Encoding.UTF8.GetBytes("\x1b[D"), false, false);
                }
                break;
            case Key.Home:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[H"), false, false);
                break;
            case Key.End:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[F"), false, false);
                break;
            case Key.Back:
                this.console.SendText(new byte[] { 0x7f }, false, false);
                break;
            case Key.Escape:
                this.console.SendText(new byte[] { 0x1b }, false, false);
                break;
            case Key.Insert:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[2~"), false, false);
                break;
            case Key.Delete:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[3~"), false, false);
                break;
            case Key.PageUp:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[5~"), false, false);
                break;
            case Key.PageDown:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[6~"), false, false);
                break;
            case Key.F1:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1bOP"), false, false);
                break;
            case Key.F2:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1bOQ"), false, false);
                break;
            case Key.F3:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1bOR"), false, false);
                break;
            case Key.F4:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1bOS"), false, false);
                break;
            case Key.F5:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[15~"), false, false);
                break;
            case Key.F6:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[17~"), false, false);
                break;
            case Key.F7:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[18~"), false, false);
                break;
            case Key.F8:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[19~"), false, false);
                break;
            case Key.F9:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[20~"), false, false);
                break;
            case Key.F10:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[21~"), false, false);
                break;
            case Key.F11:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[23~"), false, false);
                break;
            case Key.F12:
                this.console.SendText(Encoding.UTF8.GetBytes("\x1b[24~"), false, false);
                break;
            case Key.OemComma:
                if (this.IsCtrlModified)
                {
                    this.OnKeyboardShortcut(new KeyboardShortcutEventArgs { Shortcut = condo.KeyboardShortcut.OpenConfig });
                }
                break;
            default:
                e.Handled = false; // just kidding 🙃
                break;
            }
        }

        private void OnKeyboardShortcut(KeyboardShortcutEventArgs args)
        {
            this.KeyboardShortcut?.Invoke(this, args);
        }
    }
}
