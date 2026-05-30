using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages.Tasks
{
    public partial class TaskEditDialog : UserControl
    {
        public TaskEditDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
