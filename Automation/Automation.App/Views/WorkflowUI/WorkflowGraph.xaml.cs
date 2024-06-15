using Automation.App.ViewModels.Graph;
using Automation.App.ViewModels.Scopes;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowGraph.xaml
    /// </summary>
    public partial class WorkflowGraph : UserControl
    {
        // Dependency property Editor of type EditorViewModel
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
            "Editor",
            typeof(EditorViewModel),
            typeof(WorkflowGraph),
            new PropertyMetadata(null));

        public EditorViewModel Editor
        {
            get => (EditorViewModel)GetValue(EditorProperty);
            set => SetValue(EditorProperty, value);
        }

        public WorkflowGraph()
        {
            InitializeComponent();
        }
    }
}
