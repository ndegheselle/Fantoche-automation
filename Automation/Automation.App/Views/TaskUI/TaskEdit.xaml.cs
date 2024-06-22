using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        private readonly TaskNode _scope;

        public TaskEdit(TaskNode taskScope)
        {
            _scope = taskScope;
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
