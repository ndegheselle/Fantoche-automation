using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowEdit.xaml
    /// </summary>
    public partial class WorkflowEdit : UserControl, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        private readonly WorkflowNode _scope;

        public WorkflowEdit(WorkflowNode workflowScope)
        {
            _scope= workflowScope;
            InitializeComponent();
            this.DataContext = _scope;
        }

        #region UI Events
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            ModalParent?.Close();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            // New scope
            if (_scope.Id == Guid.Empty)
                _scope.Id = Guid.NewGuid();

            ModalParent?.Close(true);
        }
        #endregion
    }
}
