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
        private readonly IModalContainer _modal;
        private readonly Scope _scope;

        public ScopePage(IModalContainer modal, Scope scope)
        {
            _scope = scope;
            _modal = modal;
            InitializeComponent();
            this.DataContext = _scope;
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private async void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            Scope newScope = new Scope();
            if (await _modal.Show("Add scope", new ScopeEdit(newScope)))
                _scope.AddChild(newScope);
        }
        #endregion
    }
}
