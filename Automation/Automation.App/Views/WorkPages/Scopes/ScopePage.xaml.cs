using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Joufflu.Popups;
using Joufflu.Shared.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl, IPage
    {
        public ILayout? ParentLayout { get; set; }
        public Scope Scope { get; set; }

        private IModal _modal => this.GetCurrentModalContainer();
        
        private readonly ScopesClient _scopeClient;
        private readonly TasksClient _taskClient;

        public ScopePage(Scope scope)
        {
            _scopeClient = Services.Provider.GetRequiredService<ScopesClient>();
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
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

            Scope = fullScope;
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

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new ScopeEditModal(Scope));
        }
        #endregion

    }
}
