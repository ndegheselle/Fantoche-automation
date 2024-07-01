using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.App.Views.WorkflowUI
{
    public class WorkflowEditModal : WorkflowEdit, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit workflow", ValidButtonText = "Save" };

        public WorkflowEditModal(WorkflowNode workflowScope) : base(workflowScope)
        {
            if (workflowScope.Id == Guid.Empty)
                Options.Title = "New workflow";
        }

        public void OnModalClose(bool result)
        {
            // Not a new workflow anymore !
            if (_scope.Id == Guid.Empty)
                _scope.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEdit : UserControl
    {
        public IModalContainer? ModalParent { get; set; }
        protected readonly WorkflowNode _scope;

        public WorkflowEdit(WorkflowNode workflowScope)
        {
            _scope= workflowScope;
            InitializeComponent();
            this.DataContext = _scope;
        }

    }
}
