using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App
{
    // See App: Avalonia UI types are excluded from PropertyChanged.Fody weaving.
    [PropertyChanged.DoNotNotify]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
