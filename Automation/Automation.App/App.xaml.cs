using System.Windows;
using Automation.App.Services;

namespace Automation.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceProvider.Themes.Value.Initialize();

        var window = new MainWindow();
        window.Show();
    }
}
