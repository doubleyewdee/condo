using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace condo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // XXX: this is some garbage. tasks are a pain.
#if DEBUG
            new Timer((_) => GC.Collect(2, GCCollectionMode.Forced, true, true), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
#endif
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
