using Automation.Models.Work;
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
            typeof(BaseGraphTask),
            typeof(GraphEditorNode),
            new PropertyMetadata(null));

        public BaseGraphTask Task
        {
            get { return (BaseGraphTask)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public GraphEditorNode() { InitializeComponent(); }
    }
}
