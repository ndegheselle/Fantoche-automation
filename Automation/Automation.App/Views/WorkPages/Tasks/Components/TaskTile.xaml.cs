using Automation.Models;
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
            typeof(Automation.Shared.Data.Task.BaseAutomationTask),
            typeof(TaskTile),
            new PropertyMetadata(null));

        public Automation.Shared.Data.Task.BaseAutomationTask Task
        {
            get { return (Automation.Shared.Data.Task.BaseAutomationTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public TaskTile() { InitializeComponent(); }
    }
}