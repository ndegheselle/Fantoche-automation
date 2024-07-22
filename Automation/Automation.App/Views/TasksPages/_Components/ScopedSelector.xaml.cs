using Automation.App.Base;
using Automation.App.ViewModels.Tasks;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
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
        public event Action<ScopedItem?>? SelectedChanged;

        #region Dependency Properties
        // Dependency property Scope RootScope
        public static readonly DependencyProperty RootScopeProperty = DependencyProperty.Register(
            nameof(RootScope),
            typeof(ScopeItem),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public ScopeItem RootScope
        {
            get { return (ScopeItem)GetValue(RootScopeProperty); }
            set { SetValue(RootScopeProperty, value); }
        }

        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(ScopedItem),
            typeof(ScopedSelector),
            new PropertyMetadata(null));

        public ScopedItem? Selected
        {
            get { return (ScopedItem?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        #endregion

        #region Props

        public EnumScopedType AllowedSelectedNodes { get; set; } = EnumScopedType.Scope | EnumScopedType.Workflow | EnumScopedType.Task;

        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _client;

        #endregion

        public ScopedSelector() {
            _client = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            InitializeComponent();
        }

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedItem? selected = treeView.SelectedItem as ScopedItem;

            // Load childrens if the selected element is a scope and its childrens are not loaded
            if (selected != null && selected is ScopeItem scope && scope.Childrens.Count == 0)
            {
                Scope? fullScope = await _client.GetScopeAsync(scope.ScopeNode.Id);

                if (fullScope == null)
                    return;
                scope.RefreshChildrens(fullScope);
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
