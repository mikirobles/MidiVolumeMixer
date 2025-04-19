using System;
using System.IO;
using System.Windows;

namespace MidiVolumeMixer
{
    public partial class App : Application
    {
        public App()
        {
            // Configure global exception handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show($"An application error occurred: {e.Exception.Message}\n\nCheck error.log for details.", 
                            "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
                MessageBox.Show($"A fatal error occurred: {ex.Message}\n\nCheck error.log for details.", 
                                "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogException(Exception ex)
        {
            try
            {
                string errorMsg = $"[{DateTime.Now}] Error: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n";
                File.AppendAllText("error.log", errorMsg);
            }
            catch
            {
                // If we can't log the error, there's not much we can do
            }
        }
    }
}