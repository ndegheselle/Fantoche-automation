using ShadUI;

namespace Automation.App;

internal partial class MainWindow : Window
{
    public MainWindow(MainVM mainVm)
    {
        InitializeComponent();
        DataContext = mainVm;
    }
}