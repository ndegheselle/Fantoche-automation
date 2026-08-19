using Joufflu.Themes;
using System.Windows;
using System.Windows.Threading;

namespace Automation.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SpineViewModel? shell;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            ThemeManager.Instance.Initialize();

            shell = new SpineViewModel();
            new MainWindow(shell).Show();
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                HandleException(exception);
            }
        }

        private void HandleException(Exception exception)
        {
            try
            {
                shell?.Toasts.Error("An unexpected error happened ...", "Ooops");
            }
            catch
            { }
        }
    }
}
