namespace condo
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //ConsoleBuffer.Logger.Init(Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), @"Source\Repos\wincon\wincon.log"));
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ConsoleBuffer.Logger.Verbose($"unhandled task exception {e.Exception}");
            this.Dispatcher.InvokeAsync(() =>
            {
                throw e.Exception;
            });
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ConsoleBuffer.Logger.Verbose($"unhandled process exception {e.ExceptionObject}");
            this.Dispatcher.InvokeAsync(() =>
            {
                throw e.ExceptionObject as Exception;
            });
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ConsoleBuffer.Logger.Verbose($"unhandled dispatcher exception {e.Exception}");
            System.Windows.MessageBox.Show(e.Exception.ToString());
        }
    }
}
