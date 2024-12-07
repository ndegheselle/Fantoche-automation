using Automation.App.Shared.ViewModels.Work;
using Joufflu.Popups;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows
{
    public class WorkflowEditModal : WorkflowEdit, IModalContent
    {
        public Modal? ParentLayout { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit workflow" };

        public WorkflowEditModal(WorkflowNode workflow) : base(workflow)
        {
            if (Workflow.Id == Guid.Empty)
                Options.Title = "New workflow";
        }
    }

    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEdit : UserControl
    {
        public static readonly DependencyProperty WorkflowProperty =
            DependencyProperty.Register(nameof(Workflow), typeof(WorkflowNode), typeof(WorkflowEdit), new PropertyMetadata(null));

        public WorkflowNode Workflow
        {
            get { return (WorkflowNode)GetValue(WorkflowProperty); }
            set { SetValue(WorkflowProperty, value); }
        }

        public WorkflowEdit()
        {
            InitializeComponent();
        }

        public WorkflowEdit(WorkflowNode workflow)
        {
            Workflow = workflow;
            InitializeComponent();
        }
    }
}
