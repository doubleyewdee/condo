namespace condo
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ConsoleBuffer;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IRenderTarget
    {
        private Screen screen;
        private ConsoleWrapper console;
        private KeyHandler keyHandler;
        private Character[,] characters;
        private volatile int shouldRedraw;

        public MainWindow()
        {
            this.InitializeComponent();

            this.Loaded += this.OnLoaded;
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
        }

        private static readonly TimeSpan MaxRedrawFrequency = TimeSpan.FromMilliseconds(10);
        private readonly Stopwatch redrawWatch = new Stopwatch();
        private void UpdateContents(object sender, PropertyChangedEventArgs args)
        {
            Interlocked.CompareExchange(ref this.shouldRedraw, 1, 0);
            this.Dispatcher.InvokeAsync(() =>
            {
                if (this.redrawWatch.Elapsed < MaxRedrawFrequency)
                    return;

                this.shouldRedraw = 0;
                this.console.Buffer.Render(this);
                this.Redraw();
                this.redrawWatch.Reset();
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.screen = new Screen();
            this.screenCanvas.Children.Add(this.screen);
            this.screenCanvas.Width = this.screen.Width;
            this.screenCanvas.Height = this.screen.Height;

            this.console = TerminalManager.Instance.GetOrCreate(0, "cmd.exe");
            this.keyHandler = new KeyHandler(this.console);

            this.characters = new Character[this.console.Width, this.console.Height];

            this.console.PropertyChanged += this.UpdateContents;
            this.redrawWatch.Start();

            // this will catch trailing cases where the screen gets very full and we never get around to redrawing at the end.
            var redrawTimer = new DispatcherTimer();
            redrawTimer.Interval = TimeSpan.FromMilliseconds(50);
            redrawTimer.Tick += (_, args) =>
            {
                if (this.shouldRedraw != 0)
                {
                    this.shouldRedraw = 0;
                    this.console.Buffer.Render(this);
                    this.Redraw();
                }
            };
            redrawTimer.Start();

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
            this.console.PropertyChanged -= this.UpdateContents;
            this.console?.Dispose();
            this.console = null;
        }

        private void Redraw()
        {
            for (var x = 0; x < this.console.Width; ++x)
            {
                for (var y = 0; y < this.console.Height; ++y)
                {
                    this.screen.SetCellCharacter(x, y, (char)this.characters[x, y].Glyph);
                }
            }
        }

        public void RenderCharacter(Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }
    }
}
