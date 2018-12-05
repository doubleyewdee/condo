namespace condo
{
    using System.ComponentModel;
    using System.Security;
    using System.Windows;
    using ConsoleBuffer;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MinimumWindowsVersion = 17763;
        private readonly XtermPalette mellowPalette;
        private ConsoleWrapper console;
        private KeyHandler keyHandler;

        private bool IsOSVersionSupported()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false))
                {
                    if (key?.GetValue("CurrentBuildNumber") is string version && int.TryParse(version, out int currentBuild))
                    {
                        return currentBuild >= MinimumWindowsVersion;
                    }
                }
            }
            catch (SecurityException) { }

            return false; 
        }

        public MainWindow()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;

            // XXX: lazy dark but not painfully bright palette
            this.mellowPalette = new XtermPalette();
            this.mellowPalette[0] = new Character.ColorInfo { R = 0x1d, G = 0x1f, B = 0x21 };
            this.mellowPalette[1] = new Character.ColorInfo { R = 0xa5, G = 0x42, B = 0x42 };
            this.mellowPalette[2] = new Character.ColorInfo { R = 0x8c, G = 0x94, B = 0x40 };
            this.mellowPalette[3] = new Character.ColorInfo { R = 0xde, G = 0x93, B = 0x5f };
            this.mellowPalette[4] = new Character.ColorInfo { R = 0x5f, G = 0x81, B = 0x9d };
            this.mellowPalette[5] = new Character.ColorInfo { R = 0x85, G = 0x67, B = 0x8f };
            this.mellowPalette[6] = new Character.ColorInfo { R = 0x5e, G = 0x8d, B = 0x87 };
            this.mellowPalette[7] = new Character.ColorInfo { R = 0x70, G = 0x78, B = 0x80 };
            this.mellowPalette[8] = new Character.ColorInfo { R = 0x37, G = 0x3b, B = 0x41 };
            this.mellowPalette[9] = new Character.ColorInfo { R = 0xcc, G = 0x66, B = 0x66 };
            this.mellowPalette[10] = new Character.ColorInfo { R = 0xb5, G = 0xbd, B = 0x68 };
            this.mellowPalette[11] = new Character.ColorInfo { R = 0xf0, G = 0xc6, B = 0x74 };
            this.mellowPalette[12] = new Character.ColorInfo { R = 0x81, G = 0xa2, B = 0xbe };
            this.mellowPalette[13] = new Character.ColorInfo { R = 0xb2, G = 0x94, B = 0xbb };
            this.mellowPalette[14] = new Character.ColorInfo { R = 0x8a, G = 0xbe, B = 0xb7 };
            this.mellowPalette[15] = new Character.ColorInfo { R = 0xc5, G = 0xc8, B = 0xc6 };

            this.screen.Palette = this.mellowPalette;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!this.IsOSVersionSupported())
            {
                var msg = System.Text.Encoding.UTF8.GetBytes(
                    "\x1b[2J\x1b[H" +
                    "ConPTY APIs required for this application are not available.\r\n" +
                    $"Please update to Windows 10 1809 (build {MinimumWindowsVersion}) or higher.\r\n" +
                    "Press any key to exit.\r\n");
                this.screen.Buffer.Append(msg, msg.Length);
                this.KeyDown += (_, args) => this.Close();
                return;
            }

            this.console = TerminalManager.Instance.GetOrCreate(0, "wsl.exe");
            this.keyHandler = new KeyHandler(this.console);

#if DEBUG
            // There is currently a ... behavior ... in VS where it hijacks console output from spawned child
            // processes with no recourse to turn this off, so we don't want to bother with the console output
            // above as we'll never get any (sucks). To work around this use ctrl+f5 to launch, in debug builds
            // the debugger will attach above.
            if (!System.Diagnostics.Debugger.IsAttached)
#endif
            this.screen.Buffer = this.console.Buffer;

#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif

            this.console.Buffer.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Title")
                {
                    this.Dispatcher.InvokeAsync(() => this.Title = this.console.Buffer.Title);
                }
            };

            this.KeyDown += this.keyHandler.OnKeyDown;
            this.KeyDown += (_, args) => this.screen.VerticalOffset = double.MaxValue; // force scroll on keypress.
            this.TextInput += this.keyHandler.OnTextInput;

            this.console.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Running" && this.console != null && this.console.Running == false)
                {
                    var msg = System.Text.Encoding.UTF8.GetBytes($"\r\n[process terminated with code {this.console.ProcessExitCode}, press <enter> to exit.]");
                    this.screen.Buffer.Append(msg, msg.Length);

                    this.KeyDown += (keySender, keyArgs) =>
                    {
                        if (keyArgs.Key == System.Windows.Input.Key.Enter)
                        {
                            this.Close();
                        }
                    };
                }
            };

            this.Closing += this.HandleClosing;
        }

        private void HandleClosing(object sender, CancelEventArgs e)
        {
            this.screen.Close();
            this.screen = null;

            this.console?.Dispose();
            this.console = null;
        }
    }
}
