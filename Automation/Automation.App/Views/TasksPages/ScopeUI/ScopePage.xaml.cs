using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public Scope Scope { get; set; }

        private IModalContainer _modal => this.GetCurrentModalContainer();

        private readonly App _app = (App)App.Current;
        private readonly ScopesClient _scopeClient;
        private readonly TasksClient _taskClient;

        public ScopePage(Scope scope)
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Scope = scope;

            InitializeComponent();
            LoadFullScope(Scope.Id);
            HandleFocus();
        }

        private async void LoadFullScope(Guid scopeId)
        {
            Scope? fullScope = await _scopeClient.GetByIdAsync(scopeId);

            if (fullScope == null)
                throw new ArgumentException("Scope not found");

            Scope.Childrens = fullScope.Childrens;
            Scope.Context = fullScope.Context;
            Scope.RefreshChildrens();
        }

        private void HandleFocus()
        {
            switch (Scope.FocusOn)
            {
                case EnumScopedTabs.Settings:
                    ScopeTabControl.SelectedIndex = 1;
                    break;
            }
            Scope.FocusOn = EnumScopedTabs.Default;
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            ScopedElement? selectedElement = listBox.SelectedItem as ScopedElement;

            if (selectedElement == null)
                return;

            selectedElement.IsSelected = true;
        }
        #endregion
    }
}
