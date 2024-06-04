using Automation.App.Base;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl
    {
        private IWindowContainer _windowContainer;
        public Scope Scope { get; set; }

        public ScopePage(Scope scope)
        {
            Scope = scope;
            InitializeComponent();
            this.DataContext = Scope;

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (_windowContainer == null)
                _windowContainer = (IWindowContainer)Window.GetWindow(this);
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            _windowContainer.Modal.Show("Add Scope", new ScopeEdit());
        }
        #endregion
    }
}
