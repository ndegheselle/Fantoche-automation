using Automation.App.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Automation.App
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

            ServiceProvider.Themes.Value.Initialize();
            desktop.MainWindow = new MainWindow(ServiceProvider.Settings);

            base.OnFrameworkInitializationCompleted();
        }
    }
}