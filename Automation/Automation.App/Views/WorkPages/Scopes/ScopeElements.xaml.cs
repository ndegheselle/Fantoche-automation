using Automation.App.Shared.ViewModels.Work;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// Logique d'interaction pour ScopeElements.xaml
    /// </summary>
    public partial class ScopeElements : UserControl
    {
        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScopeProperty =
            DependencyProperty.Register(nameof(Scope), typeof(Scope), typeof(ScopeElements), new PropertyMetadata(null));

        public Scope Scope
        {
            get { return (Scope)GetValue(ScopeProperty); }
            set { SetValue(ScopeProperty, value); }
        }

        public ScopedElement? Selected { get; set; }

        public ScopeElements()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Selected == null)
                return;
            Selected.ExpandParent();
            Selected.IsSelected = true;
        }
    }
}
