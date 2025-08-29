using Automation.Models;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour GraphEditorNode.xaml
    /// </summary>
    public partial class GraphEditorNode : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(GraphTask),
            typeof(GraphEditorNode),
            new PropertyMetadata(null));

        public GraphTask Task
        {
            get { return (GraphTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public GraphEditorNode() { InitializeComponent(); }
    }
}
