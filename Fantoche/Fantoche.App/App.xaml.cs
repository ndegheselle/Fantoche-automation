using System.Windows;
using System.Windows.Threading;
using Joufflu.Themes;

namespace Fantoche.App
{
    public partial class App : Application
    {
        private SpineViewModel? shell;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            shell = SpineViewModel.Instance;

            // Restored before the first window shows, so it opens with the right theme.
            ThemeManager.Instance.Theme = shell.Settings.Theme;
            ThemeManager.Instance.Initialize();

            new MainWindow(shell).Show();
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
                HandleException(exception);
        }

        private void HandleException(Exception exception)
        {
            try
            {
                shell?.Toasts.Error(exception.Message, "Ooops");
            }
            catch
            { }
        }
    }
}
