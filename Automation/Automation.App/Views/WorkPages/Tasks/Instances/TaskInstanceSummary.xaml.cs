using Automation.Models.Work;
using Automation.Shared.Data.Execution;
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
            typeof(NodeInstance),
            typeof(TaskInstanceSummary),
            new PropertyMetadata(null));

        public NodeInstance? Instance
        {
            get { return (NodeInstance?)GetValue(InstanceProperty); }
            set { SetValue(InstanceProperty, value); }
        }

        public TaskInstanceSummary()
        {
            InitializeComponent();
        }
    }
}
