namespace condo
{
    using System.ComponentModel;
    using System.Windows;
    using ConsoleBuffer;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConsoleWrapper console;
        private KeyHandler keyHandler;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.console = TerminalManager.Instance.GetOrCreate(0, "wsl.exe");
            this.keyHandler = new KeyHandler(this.console);

#if DEBUG
            // There is currently a ... behavior ... in VS where it hijacks console output from spawned child
            // processes with no recourse to turn this off, so we don't want to bother with the console output
            // above as we'll never get any (sucks). To work around this use ctrl+f5 to launch, in debug builds
            // the debugger will attach above.
            if (!System.Diagnostics.Debugger.IsAttached)
#endif
            this.screen = new Screen(this.console.Buffer);
            this.scrollViewer.Content = this.screen;

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
