using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
using Automation.Shared;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
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
        private readonly ScopeClient _scopeClient;
        private readonly TaskClient _taskClient;

        public ScopePage(Scope scope)
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopeClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<TaskClient>();
            Scope = scope;

            InitializeComponent();
            LoadFullScope(Scope.Id);
        }

        public async void LoadFullScope(Guid scopeId)
        {
            Scope? fullScope = await _scopeClient.GetByIdAsync(scopeId);

            if (fullScope == null)
                throw new ArgumentException("Scope not found");

            Scope.Childrens = fullScope.Childrens;
            Scope.Context = fullScope.Context;
            Scope.RefreshChildrens();
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private async void ButtonParameters_Click(object sender, RoutedEventArgs e)
        {
            if (Scope == null)
                return;
            await _modal.Show(new ScopeEditModal(Scope));
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
