using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEdit : UserControl, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        private readonly WorkflowNode _scope;

        public WorkflowEdit(WorkflowNode workflowScope)
        {
            _scope= workflowScope;
            InitializeComponent();
            this.DataContext = _scope;
        }

        public void OnModalClose(bool result)
        {
            // New scope
            if (_scope.Id == Guid.Empty)
                _scope.Id = Guid.NewGuid();
        }
    }
}
