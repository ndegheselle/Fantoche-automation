using Automation.App.ViewModels.Workflow.Editor;
using Joufflu.Popups;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEditorOverlay.xaml
    /// </summary>
    public partial class WorkflowEditorOverlay : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
            nameof(Editor),
            typeof(WorkflowEditorCommands),
            typeof(WorkflowEditorOverlay),
            new PropertyMetadata(null));
        #endregion

        public WorkflowEditorViewModel Editor
        {
            get { return (WorkflowEditorViewModel)GetValue(EditorProperty); }
            set { SetValue(EditorProperty, value); }
        }

        private IModal _modal => this.GetCurrentModalContainer();

        public WorkflowEditorOverlay()
        {
            InitializeComponent();
        }

        private void OpenHelp_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new WorkflowEditorHelp());
        }
    }
}