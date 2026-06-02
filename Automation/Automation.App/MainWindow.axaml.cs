using ShadUI;

namespace Automation.App;

public partial class MainWindow : Window
{
    public MainWindow(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        DataContext = settingsViewModel;
    }
}