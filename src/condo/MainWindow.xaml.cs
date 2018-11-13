namespace condo
{
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ConsoleBuffer.IRenderTarget
    {
        private DpiScale dpiInfo;
        private ConsoleBuffer.ConsoleWrapper console;
        private ConsoleBuffer.Character[,] characters;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += this.OnLoaded;

            this.console = TerminalManager.Instance.GetOrCreate(0, "ping -t localhost");
            System.Diagnostics.Debugger.Launch();
            this.console.PropertyChanged += this.UpdateContents;
        }

        private void UpdateContents(object sender, PropertyChangedEventArgs args)
        {
            this.console.Buffer.Render(this);
            Dispatcher.InvokeAsync(() =>  this.Redraw());
        }

        private Size DetermineSize()
        {
            DpiScale dpi = this.dpiInfo;

            // because we only ever expect to work with monospace fonts we can extrapolate from any single character.
            // lord help if someone gets real excited about proportional font console.
            var sampleText = new FormattedText("x", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                               new Typeface(this.stuff.FontFamily, this.stuff.FontStyle, this.stuff.FontWeight, this.stuff.FontStretch),
                                               this.stuff.FontSize, Brushes.Black, dpi.PixelsPerDip);

            return new Size(sampleText.Width * this.console.Width, sampleText.Height * this.console.Height);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.dpiInfo = VisualTreeHelper.GetDpi(this);

            var stuffSize = this.DetermineSize();
            this.stuff.Height = stuffSize.Height;
            this.stuff.Width = stuffSize.Width;

            this.characters = new ConsoleBuffer.Character[this.console.Height, this.console.Width];
            this.Redraw();
        }

        private void Redraw()
        {
            var sb = new StringBuilder();
            for (var x = 0; x < this.console.Height; ++x)
            {
                for (var y = 0; y < this.console.Width; ++y)
                {
                    sb.Append((char)this.characters[x, y].Glyph);
                }
                sb.Append('\n');
            }

            this.stuff.Text = sb.ToString();
        }

        public void RenderCharacter(ConsoleBuffer.Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }
    }
}
