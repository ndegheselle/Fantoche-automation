using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Components.Inputs
{
    public partial class SelectColorDialog : UserControl
    {
        public SelectColorDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
