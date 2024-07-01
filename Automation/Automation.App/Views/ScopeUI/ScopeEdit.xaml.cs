using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.ScopeUI
{
    public class ScopeEditModal : ScopeEdit, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scope", ValidButtonText = "Save" };

        public ScopeEditModal(Scope scope) : base(scope)
        {
            if (scope.Id == Guid.Empty)
                Options.Title = "New scope";
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
    public partial class ScopeEdit : UserControl
    {
        protected readonly Scope _scope;

        public ScopeEdit(Scope scope)
        {
            _scope = scope;
            this.DataContext = _scope;
            InitializeComponent();
        }
    }
}
