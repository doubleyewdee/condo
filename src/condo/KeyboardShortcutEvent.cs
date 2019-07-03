namespace condo
{
    using System;

    enum KeyboardShortcut
    {
        OpenConfig = 0,
    }

    sealed class KeyboardShortcutEventArgs : EventArgs
    {
        public KeyboardShortcut Shortcut { get; set; }
    }
}
