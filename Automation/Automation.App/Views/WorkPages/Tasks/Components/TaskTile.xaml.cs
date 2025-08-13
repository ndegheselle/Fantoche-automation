using Automation.Dal.Models;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskTile.xaml
    /// </summary>
    public partial class TaskTile : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(BaseAutomationTask),
            typeof(TaskTile),
            new PropertyMetadata(null));

        public BaseAutomationTask Task
        {
            get { return (BaseAutomationTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public TaskTile() { InitializeComponent(); }
    }
}