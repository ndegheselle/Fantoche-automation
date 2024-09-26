using Automation.App.Shared.ViewModels.Tasks;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    public class WorkflowEditModal : WorkflowEdit, IModalContentValidation
    {
        public ILayout? ParentLayout { get; set; }
        public ModalValidationOptions Options => new ModalValidationOptions() { Title = "Edit workflow", ValidButtonText = "Save" };

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
