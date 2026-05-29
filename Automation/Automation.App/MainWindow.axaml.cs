using Avalonia.Markup.Xaml;

namespace Automation.App
{
    // ShadUI.Window provides the themed window chrome (replaces AdonisUI's AdonisWindow).
    public partial class MainWindow : ShadUI.Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
