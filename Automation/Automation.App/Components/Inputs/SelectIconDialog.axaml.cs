using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Components.Inputs
{
    public partial class SelectIconDialog : UserControl
    {
        public SelectIconDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
