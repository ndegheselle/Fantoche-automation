using Automation.App.Base;
using Automation.App.Shared.ViewModels.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    public class ScopeEditModal : ScopeEdit, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scoped", ValidButtonText = "Save" };

        public ScopeEditModal(Scope scope) : base(scope)
        {
            if (scope.Id == Guid.Empty)
                Options.Title = "New scoped";
        }
    }


    /// <summary>
    /// Logique d'interaction pour ScopedParameters.xaml
    /// </summary>
    public partial class ScopeEdit : UserControl
    {
        public static readonly DependencyProperty ScopedProperty =
            DependencyProperty.Register(
            nameof(Scope),
            typeof(Scope),
            typeof(ScopeEdit),
            new PropertyMetadata(null));

        public Scope Scope
        {
            get { return (Scope)GetValue(ScopedProperty); }
            set { SetValue(ScopedProperty, value); }
        }

        public ScopeEdit(Scope scoped)
        {
            Scope = scoped;
            InitializeComponent();
        }

        public ScopeEdit() { InitializeComponent(); }
    }
}
