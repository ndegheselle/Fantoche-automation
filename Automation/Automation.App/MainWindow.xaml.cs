using Automation.App.Services;
using Wpf.Ui.Controls;

namespace Automation.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = ServiceProvider.Main;
    }
}
