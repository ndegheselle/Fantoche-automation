using System.Windows;
using System.Windows.Threading;
using Joufflu.Themes;

namespace Fantoche.App
{
    public partial class App : Application
    {
        private const string ErrorTitle = "Ooops";

        private SpineViewModel? shell;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            shell = SpineViewModel.Instance;
            Resources.MergedDictionaries.Add(shell.ViewTemplates);

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
            if (shell == null)
                return;

            try
            {
                // A toast failing to render raises the same exception again : toasting it once more
                // would loop, piling up toasts until the application hangs.
                if (shell.Toasting.Toasts.Any(x => x.Title == ErrorTitle && x.Message == exception.Message))
                    return;

                shell.Toasting.Error(exception.Message, ErrorTitle);
            }
            catch
            { }
        }
    }
}
