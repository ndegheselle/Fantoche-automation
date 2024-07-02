using Automation.App.Base;
using Automation.App.ViewModels.Graph;
using Automation.Base.ViewModels;
using Automation.Supervisor.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    public class NodeSelectorModal : NodeSelector, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions? Options => new ModalOptions() { Title = "Add node", ValidButtonText = "Add" };
    }

    /// <summary>
    /// Logique d'interaction pour ScopedElementSelector.xaml
    /// </summary>
    public partial class NodeSelector : UserControl
    {
        #region Dependency Properties
        // Dependency property Scope RootScope
        public static readonly DependencyProperty RootScopeProperty = DependencyProperty.Register(
            nameof(RootScope),
            typeof(Scope),
            typeof(NodeSelector),
            new PropertyMetadata(null));

        public Scope RootScope
        {
            get { return (Scope)GetValue(RootScopeProperty); }
            set { SetValue(RootScopeProperty, value); }
        }

        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(Node),
            typeof(NodeSelector),
            new PropertyMetadata(null));

        public Node? Selected
        {
            get { return (Node?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        #endregion

        #region Props

        public EnumNodeType AllowedSelectedNodes { get; set; } = EnumNodeType.Scope | EnumNodeType.Workflow | EnumNodeType.Task;

        #endregion

        public NodeSelector() {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            Node? selected = treeView.SelectedItem as Node;

            // Load childrens if the selected element is a scope and its childrens are not loaded
            if (selected != null && selected is Scope scope && scope.Childrens.Count == 0)
            {
                ScopeRepository scopeRepository = new ScopeRepository();
                Scope? fullScope = scopeRepository.GetNode(selected.Id) as Scope;

                foreach (Node child in fullScope.Childrens)
                    scope.AddChild(child);
            }

            if (selected != null && !AllowedSelectedNodes.HasFlag(selected.Type))
            {
                selected.IsSelected = false;
                return;
            }
            Selected = selected;
        }
    }
}
