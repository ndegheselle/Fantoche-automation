using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        private readonly TaskNode _scope;

        public TaskEdit(TaskNode taskScope)
        {
            _scope = taskScope;
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
