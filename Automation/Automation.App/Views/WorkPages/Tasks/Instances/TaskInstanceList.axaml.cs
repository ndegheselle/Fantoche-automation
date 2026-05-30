using Automation.Shared.Data.Execution;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// MIGRATION: list of task instances (was a WPF DataGrid; now a templated ListBox with a header
    /// row to avoid the Avalonia DataGrid package + theming). State -> icon/colour via converters.
    /// Double-clicking raises <see cref="InstanceActivated"/>; the host opens the detail dialog
    /// (InstanceDetailModal not yet ported).
    /// </summary>
    public partial class TaskInstanceList : UserControl
    {
        public static readonly StyledProperty<IEnumerable<TaskInstance>?> InstancesProperty =
            AvaloniaProperty.Register<TaskInstanceList, IEnumerable<TaskInstance>?>(nameof(Instances));

        public IEnumerable<TaskInstance>? Instances
        {
            get => GetValue(InstancesProperty);
            set => SetValue(InstancesProperty, value);
        }

        /// <summary>Raised when an instance row is double-clicked.</summary>
        public event Action<TaskInstance>? InstanceActivated;

        public TaskInstanceList()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is ListBox { SelectedItem: TaskInstance instance })
                InstanceActivated?.Invoke(instance);
        }
    }
}
