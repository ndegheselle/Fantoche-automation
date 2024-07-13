using Automation.App.Base;
using Automation.Shared.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.ScopeUI
{
    public class ScopedEditModal : ScopedEdit, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scoped", ValidButtonText = "Save" };

        public ScopedEditModal(ScopedElement scope) : base(scope)
        {
            if (scope.Id == Guid.Empty)
                Options.Title = "New scoped";
        }

        public void OnModalClose(bool result)
        {
            // Not a new scope anymore !
            if (_scope.Id == Guid.Empty)
                _scope.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Logique d'interaction pour ScopeEdit.xaml
    /// </summary>
    public partial class ScopedEdit : UserControl
    {
        protected readonly ScopedElement _scope;

        public ScopedEdit(ScopedElement scope)
        {
            _scope = scope;
            this.DataContext = _scope;
            InitializeComponent();
        }
    }
}
