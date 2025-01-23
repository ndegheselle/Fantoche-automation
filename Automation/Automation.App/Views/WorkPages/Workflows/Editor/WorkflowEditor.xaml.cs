using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEditor.xaml
    /// </summary>
    public partial class WorkflowEditor : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty WorkflowProperty = DependencyProperty.Register(
            nameof(Workflow),
            typeof(WorkflowNode),
            typeof(WorkflowEditor),
            new PropertyMetadata(null, (o, d) => ((WorkflowEditor)o).OnWorkflowChanged()));
        #endregion

        public WorkflowNode Workflow
        {
            get { return (WorkflowNode)GetValue(WorkflowProperty); }
            set { SetValue(WorkflowProperty, value); }
        }

        public EditorViewModel? Editor { get; private set; }

        public WorkflowEditor() { InitializeComponent(); }

        private void OnWorkflowChanged()
        {
            if (Workflow == null)
                return;
            Editor = new EditorViewModel(Workflow);
        }
    }
}
