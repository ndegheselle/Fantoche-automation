using Automation.App.ViewModels.Scoped;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages.Tasks
{
    public partial class TaskPage : UserControl
    {
        public TaskPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                if (DataContext is TaskPageViewModel { Task.FocusOn: EnumScopedTab.Settings } vm)
                {
                    vm.Task.FocusOn = EnumScopedTab.Default;
                    vm.OpenSettings();
                }
            };
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
