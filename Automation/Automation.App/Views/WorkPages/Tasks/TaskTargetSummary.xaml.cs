using Automation.Dal.Models;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// Logique d'interaction pour TaskTargetSummary.xaml
    /// </summary>
    public partial class TaskTargetSummary : UserControl
    {
        public static readonly DependencyProperty TaskTargetProperty = DependencyProperty.Register(
            nameof(Target),
            typeof(TaskTarget),
            typeof(TaskTargetSummary),
            new PropertyMetadata(null));

        public TaskTarget? Target
        {
            get { return (TaskTarget?)GetValue(TaskTargetProperty); }
            set { SetValue(TaskTargetProperty, value); }
        }

        public TaskTargetSummary() { InitializeComponent(); }
    }
}
