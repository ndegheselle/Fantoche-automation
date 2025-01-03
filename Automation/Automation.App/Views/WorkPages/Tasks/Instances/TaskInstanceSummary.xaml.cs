using Automation.App.Shared.ViewModels.Work;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskInstanceSummary.xaml
    /// </summary>
    public partial class TaskInstanceSummary : UserControl
    {
        public static readonly DependencyProperty InstanceProperty = DependencyProperty.Register(
            nameof(Instance),
            typeof(TaskInstance),
            typeof(TaskInstanceSummary),
            new PropertyMetadata(null));

        public TaskInstance? Instance
        {
            get { return (TaskInstance?)GetValue(InstanceProperty); }
            set { SetValue(InstanceProperty, value); }
        }

        public TaskInstanceSummary()
        {
            InitializeComponent();
        }
    }
}
