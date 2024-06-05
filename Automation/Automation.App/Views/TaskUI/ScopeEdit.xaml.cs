using Automation.App.Base;
using Automation.App.ViewModels;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour ScopeEdit.xaml
    /// </summary>
    public partial class ScopeEdit : UserControl, IModalContent
    {
        private readonly Scope _scope;
        public IModalContainer? ModalParent { get; set; }

        public ScopeEdit(Scope scope)
        {
            _scope = scope;
            InitializeComponent();
            this.DataContext = _scope;
        }

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
    }
}
