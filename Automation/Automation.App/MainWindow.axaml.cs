using Automation.App.Services;
using ShadUI;

namespace Automation.App;

internal partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = ServiceProvider.Main;
        InitializeComponent();
    }
}