using Automation.Models;
using Joufflu.Popups;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Schedule
{
    /// <summary>
    /// Logique d'interaction pour TaskSchedule.xaml
    /// </summary>
    public partial class TaskSchedules : UserControl
    {
        public static readonly DependencyProperty TaskProperty = DependencyProperty.Register(
            nameof(Task),
            typeof(Automation.Shared.Data.Task.AutomationTask),
            typeof(TaskSchedules),
            new PropertyMetadata(default(Automation.Shared.Data.Task.AutomationTask), (o, e) => ((TaskSchedules)o).OnTargetChange()));

        public Automation.Shared.Data.Task.AutomationTask Task { get { return (Automation.Shared.Data.Task.AutomationTask)GetValue(TaskProperty); } set { SetValue(TaskProperty, value); } }
        private IModal _modal => this.GetCurrentModal();

        public TaskSchedules() { InitializeComponent(); }

        private void OnTargetChange()
        {
            // TODO load task schedule
        }
    }
}
