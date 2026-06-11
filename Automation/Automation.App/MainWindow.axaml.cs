using ShadUI;

namespace Automation.App;

internal partial class MainWindow : Window
{
    public MainWindow(MainVm mainVm)
    {
        InitializeComponent();
        DataContext = mainVm;
    }
}