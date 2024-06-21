using Automation.App.ViewModels.Graph;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    // XXX : TODO : Add on demand loading
    /// <summary>
    /// Logique d'interaction pour ScopedElementSelector.xaml
    /// </summary>
    public partial class NodeSelector : UserControl
    {
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

        public NodeSelector() {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            NodeUIWrapper? contextElement = treeView.SelectedItem as NodeUIWrapper;
            Selected = contextElement?.Node;

            if (contextElement is ScopeUIWrapper scope && scope.Childrens == null)
            {
                // Load the children of the selected scope
            }
        }
    }
}
