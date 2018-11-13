namespace condo
{
    using System;
    using System.ComponentModel;
    // The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409
    using System.Text;
    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, ConsoleBuffer.IRenderTarget
    {
        private ConsoleBuffer.ConsoleWrapper console;
        private ConsoleBuffer.Character[,] characters;

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += this.OnLoaded;

            this.console = TerminalManager.Instance.GetOrCreate(0, "ping -t localhost");
            this.characters = new ConsoleBuffer.Character[this.console.Height, this.console.Width];
            //System.Diagnostics.Debugger.Launch();
        }

        private void UpdateContents(object sender, PropertyChangedEventArgs args)
        {
            this.console.Buffer.Render(this);
            this.Dispatcher.TryRunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.Redraw());
        }

        private Size DetermineSize()
        {
            var measureText = new TextBlock { Text = "x", FontFamily = new FontFamily("Consolas"), FontSize = 12 };
            measureText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return new Size(measureText.DesiredSize.Width * this.console.Width, measureText.DesiredSize.Height * this.console.Height);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var stuffSize = this.DetermineSize();
            this.stuff.Height = stuffSize.Height;
            this.stuff.Width = stuffSize.Width;
            this.console.PropertyChanged += this.UpdateContents;
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

            //this.stuff.Text = sb.ToString();
        }

        public void RenderCharacter(ConsoleBuffer.Character c, int x, int y)
        {
            this.characters[x, y] = c;
        }
    }
}
