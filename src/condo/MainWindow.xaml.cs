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
