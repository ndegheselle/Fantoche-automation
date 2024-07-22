using Automation.App.Base;
using Automation.App.ViewModels.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    public class ScopeEditModal : ScopeEdit, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit scoped", ValidButtonText = "Save" };

        public ScopeEditModal(ScopeItem scope) : base(scope)
        {
            if (scope.ScopeNode.Id == Guid.Empty)
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
            typeof(ScopeItem),
            typeof(ScopeEdit),
            new PropertyMetadata(null));

        public ScopeItem Scope
        {
            get { return (ScopeItem)GetValue(ScopedProperty); }
            set { SetValue(ScopedProperty, value); }
        }

        public ScopeEdit(ScopeItem scoped)
        {
            Scope = scoped;
            InitializeComponent();
        }

        public ScopeEdit() { InitializeComponent(); }
    }
}
