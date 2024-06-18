using Automation.App.ViewModels.Scopes;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedElementSelector.xaml
    /// </summary>
    public partial class ScopedElementSelector : UserControl
    {
        // Dependency property Scope RootScope
        public static readonly DependencyProperty RootScopeProperty = DependencyProperty.Register(
            nameof(RootScope),
            typeof(Scope),
            typeof(ScopedElementSelector),
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
            typeof(ScopedElementSelector),
            new PropertyMetadata(null));

        public ScopedElement? Selected
        {
            get { return (ScopedElement?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public ScopedElementSelector() {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? contextElement = treeView.SelectedItem as ScopedElement;
            Selected = contextElement;
        }
    }
}
