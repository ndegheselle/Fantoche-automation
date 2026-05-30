using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkersPages
{
    public partial class WorkerMainPage : UserControl
    {
        public WorkerMainPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
