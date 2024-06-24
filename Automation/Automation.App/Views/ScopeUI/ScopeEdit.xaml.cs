using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.ScopeUI
{
    /// <summary>
    /// Logique d'interaction pour ScopeEdit.xaml
    /// </summary>
    public partial class ScopeEdit : UserControl, IModalContentCallback
    {
        private readonly Scope _scope;
        public IModalContainer? ModalParent { get; set; }

        public ScopeEdit(Scope scope)
        {
            _scope = scope;
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
