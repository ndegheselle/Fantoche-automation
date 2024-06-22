using Automation.App.ViewModels.Graph;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
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
            new PropertyMetadata(null, (e, d) => ((NodeSelector)e).OnRootScopeChange()));

        public Scope RootScope
        {
            get { return (Scope)GetValue(RootScopeProperty); }
            set { SetValue(RootScopeProperty, value); }
        }

        private void OnRootScopeChange()
        {
            if (RootScope != null)
            {
                RootScopeWrapper = new ScopeWrapper(RootScope);
            }
        }

        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(Node),
            typeof(NodeSelector),
            new PropertyMetadata(null));

        public NodeWrapper? Selected
        {
            get { return (NodeWrapper?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        #endregion

        public ScopeWrapper? RootScopeWrapper { get; set; }

        public NodeSelector() {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            NodeWrapper? contextElement = treeView.SelectedItem as NodeWrapper;
            Selected = contextElement;
        }
    }
}
