using ShadUI;

namespace Automation.App;

internal partial class MainWindow : Window
{
    public MainWindow(MainViewModel settingsViewModel)
    {
        InitializeComponent();
        DataContext = settingsViewModel;
    }
}