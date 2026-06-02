using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShadUI;

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
            desktop.MainWindow = new MainWindow(ServiceProvider.Settings.Value);
            
            base.OnFrameworkInitializationCompleted();
        }
    }
}