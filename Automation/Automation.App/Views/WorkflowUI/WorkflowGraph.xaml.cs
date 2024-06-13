using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowGraph.xaml
    /// </summary>
    public partial class WorkflowGraph : UserControl
    {
        // Dependency property WorkflowScope
        public static readonly DependencyProperty WorkflowScopeProperty = DependencyProperty.Register(
            "WorkflowScope",
            typeof(WorkflowScope),
            typeof(WorkflowGraph),
            new PropertyMetadata(null));

        public WorkflowScope WorkflowScope
        {
            get { return (WorkflowScope)GetValue(WorkflowScopeProperty); }
            set { SetValue(WorkflowScopeProperty, value); }
        }

        public WorkflowGraph() { InitializeComponent(); }
    }
}
