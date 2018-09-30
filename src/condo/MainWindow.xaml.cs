namespace condo
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.stuff.Text = "sdkjashfgdjkas";
            var terminal = TerminalManager.Instance.GetOrCreate(0, "ping -t localhost");
            terminal.PropertyChanged += this.UpdateContents;
        }

        private void UpdateContents(object sender, PropertyChangedEventArgs args)
        {
            var con = sender as ConsoleBuffer.ConsoleWrapper;
            if (sender == null && args.PropertyName != "Content")
            {
                return; // XXX: log?
            }

            Dispatcher.InvokeAsync(() =>
            {
                this.stuff.Text = con.Contents;
            });
        }
    }
}
