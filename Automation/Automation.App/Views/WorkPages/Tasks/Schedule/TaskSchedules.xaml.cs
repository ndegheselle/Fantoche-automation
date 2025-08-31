using Automation.Models.Work;
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
            typeof(AutomationTask),
            typeof(TaskSchedules),
            new PropertyMetadata(default(AutomationTask), (o, e) => ((TaskSchedules)o).OnTargetChange()));

        public AutomationTask Task { get { return (AutomationTask)GetValue(TaskProperty); } set { SetValue(TaskProperty, value); } }
        private IModal _modal => this.GetCurrentModal();

        public TaskSchedules() { InitializeComponent(); }

        private void OnTargetChange()
        {
            // TODO load task schedule
        }
    }
}
