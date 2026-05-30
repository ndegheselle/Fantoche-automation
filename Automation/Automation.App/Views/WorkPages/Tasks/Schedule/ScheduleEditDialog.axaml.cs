using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages.Tasks.Schedules
{
    public partial class ScheduleEditDialog : UserControl
    {
        public ScheduleEditDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
