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
        private Screen screen;
        private ConsoleWrapper console;
        private KeyHandler keyHandler;

        public MainWindow()
        {
            this.InitializeComponent();

            this.Loaded += this.OnLoaded;
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.console = TerminalManager.Instance.GetOrCreate(0, "bash.exe");
            this.keyHandler = new KeyHandler(this.console);

            this.screen = new Screen(this.console);
            this.screenCanvas.Children.Add(this.screen);
            this.screenCanvas.Width = this.screen.Width;
            this.screenCanvas.Height = this.screen.Height;

            this.console.Buffer.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Title")
                {
                    this.Dispatcher.InvokeAsync(() => this.Title = this.console.Buffer.Title);
                }
            };

            this.KeyDown += this.keyHandler.OnKeyDown;
            this.TextInput += this.keyHandler.OnTextInput;

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
