namespace condo
{
    using System;
    using System.ComponentModel;
    using System.Security;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using ConsoleBuffer;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand CustomRoutedF10Event = new RoutedCommand(); /**> Used to get F10 properly processed. Otherwise it is filtered by the WPF default usage (MainWindow Menu) */
        private const int MinimumWindowsVersion = 17763;
        private ConsoleWrapper console;
        private KeyHandler keyHandler;
        private Configuration configuration;

        /// <summary>
        /// Event handler to catch custom routed F10 event from MainWindow InputBindings and route it to OnKeyDown event handler we usually use
        /// </summary>
        /// <param name="sender">The Main Window</param>
        /// <param name="e">Nobody cares</param>
        private void ExecutedCustomF10Event(object sender, ExecutedRoutedEventArgs e)
        {
            /* F10 was pressed */
            this.keyHandler.OnKeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.F10) { RoutedEvent = Keyboard.KeyDownEvent });
        }

        /// <summary>
        /// CanExecuteCustomF10Event that only returns true if the source is a control.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">e.Source -> MainWindow -> Which is a control</param>
        private void CanExecuteCustomF10Event(object sender, CanExecuteRoutedEventArgs e)
        {
            var target = e.Source as Control;

            if (target != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

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

            this.InitializeWindowSizeHandling();
            this.HandleConfigurationChanged(null, null);

            this.console = TerminalManager.Instance.GetOrCreate(0, "wsl.exe");
            this.keyHandler = new KeyHandler(this.console);
            this.keyHandler.KeyboardShortcut += this.HandleKeyboardShortcut;

            this.screen.Buffer = this.console.Buffer;

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
            // XXX: a very hacky paste!
            this.MouseRightButtonDown += (_, args) =>
            {
                var text = Clipboard.GetData(DataFormats.Text) as string;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    this.console.SendText(System.Text.Encoding.UTF8.GetBytes(text));
                }
            };

            this.console.PropertyChanged += this.HandleConsoleClosed;

            this.Closing += this.HandleClosing;
        }

        private double windowFrameWidth, windowFrameHeight;
        private void InitializeWindowSizeHandling()
        {
            // XXX: maybe want to force a redraw of buffer too idk yet
            this.DpiChanged += (sender, args) => VisualTreeHelper.SetRootDpi(this, args.NewDpi);

            this.windowFrameWidth = this.ActualWidth - this.grid.ActualWidth;
            this.windowFrameHeight = this.ActualHeight - this.grid.ActualHeight;

            this.MinWidth = this.ActualWidth;
            this.MinHeight = this.ActualHeight;
        }

        private void HandleClosing(object sender, CancelEventArgs e)
        {
            this.screen.Close();
            this.screen = null;

            this.console?.Dispose();
            this.console = null;
        }

        private void HandleKeyboardShortcut(object sender, KeyboardShortcutEventArgs args)
        {
            switch (args.Shortcut)
            {
            case KeyboardShortcut.OpenConfig:
                this.configuration.ShellOpen();
                break;
            }
        }

        private void HandleConfigurationChanged(object sender, EventArgs args)
        {
            var currentConfig = (Configuration)sender;

            this.configuration = Configuration.Load(currentConfig != null ? currentConfig.Filename : Configuration.GetDefaultFilename());
            this.configuration.Changed += this.HandleConfigurationChanged;
            this.screen.SetConfiguration(this.configuration);
            // XXX: this is hacky but keeps things from being default ugly. ideally want to snap to cells while resizing window.
            this.Dispatcher.BeginInvoke(new Action(() => this.grid.Background = new SolidColorBrush(new Color { R = this.configuration.Palette[0].R, G = this.configuration.Palette[0].G, B = this.configuration.Palette[0].B, A = 255 })));
            if (currentConfig != null)
            {
                currentConfig.Changed -= this.HandleConfigurationChanged;
                currentConfig.Dispose();
            }
        }

        private void HandleConsoleClosed(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Running" && this.console != null && this.console.Running == false)
            {
                if (this.configuration.ShowProcessExitOnClose)
                {
                    var msg = System.Text.Encoding.UTF8.GetBytes($"\r\n[process terminated with code {this.console.ProcessExitCode}, press <enter> to exit.]");
                    this.screen?.Buffer.Append(msg, msg.Length);

                    this.KeyDown += (keySender, keyArgs) =>
                    {
                        if (keyArgs.Key == System.Windows.Input.Key.Enter)
                        {
                            this.Close();
                        }
                    };
                }
                else
                {
                    this.Dispatcher.Invoke(() => this.Close());
                }
            }
        }
    }
}
