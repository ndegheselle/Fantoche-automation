using System.Windows;
using Joufflu.Themes;

namespace Automation.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.Instance.Initialize();


            var shell = new ShellViewModel();
            new MainWindow { DataContext = shell }.Show();
        }
    }
}
