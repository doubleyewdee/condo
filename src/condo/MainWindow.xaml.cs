using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace condo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConsoleBuffer.ConsoleWrapper consoleWrapper;

        public MainWindow()
        {
            InitializeComponent();

            this.consoleWrapper = new ConsoleBuffer.ConsoleWrapper("debian run yes");
            this.stuff.DataContext = this.consoleWrapper;
            this.stuff.Text = this.consoleWrapper.Contents;
        }
    }
}
