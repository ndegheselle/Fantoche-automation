using Automation.App.Base;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.Components
{
    public class ScopedSelectorModal : ScopedSelector, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions? Options => new ModalOptions() { Title = "Add node", ValidButtonText = "Add" };
    }

    /// <summary>
    /// Logique d'interaction pour ScopedElementSelector.xaml
    /// </summary>
    public partial class ScopedSelector : UserControl
    {
        public event Action<ScopedElement?>? SelectedChanged;

        #region Dependency Properties
        // Dependency property Scope RootScope
        public static readonly DependencyProperty RootScopeProperty = DependencyProperty.Register(
            nameof(RootScope),
            typeof(Scope),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public Scope RootScope
        {
            get { return (Scope)GetValue(RootScopeProperty); }
            set { SetValue(RootScopeProperty, value); }
        }

        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(ScopedElement),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public ScopedElement? Selected
        {
            get { return (ScopedElement?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        #endregion

        #region Props

        public EnumScopedType AllowedSelectedNodes { get; set; } = EnumScopedType.Scope | EnumScopedType.Workflow | EnumScopedType.Task;

        private readonly App _app = (App)App.Current;
        private readonly IScopeRepository<Scope> _client;

        #endregion

        public ScopedSelector() {
            _client = _app.ServiceProvider.GetRequiredService<IScopeRepository<Scope>>();
            InitializeComponent();
        }

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? selected = treeView.SelectedItem as ScopedElement;

            // Load childrens if the selected element is a scope and its childrens are not loaded
            if (selected != null && selected is Scope scope && scope.Childrens.Count == 0)
            {
                Scope? fullScope = await _client.GetByIdAsync(scope.Id);

                if (fullScope == null)
                    return;
                scope.Childrens = fullScope.Childrens;
            }

            if (selected != null && !AllowedSelectedNodes.HasFlag(selected.Type))
            {
                selected.IsSelected = false;
                return;
            }
            Selected = selected;
            SelectedChanged?.Invoke(Selected);
        }
    }
}
