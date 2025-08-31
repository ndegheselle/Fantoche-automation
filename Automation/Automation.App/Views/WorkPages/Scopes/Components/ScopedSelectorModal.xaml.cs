using Automation.App.Shared.ApiClients;
using Automation.Models;
using Automation.Models.Work;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedSelectorModal.xaml
    /// </summary>
    public partial class ScopedSelectorModal : UserControl, IModalContent, INotifyPropertyChanged
    {
        public IModal? ParentLayout { get; set; }

        public ModalOptions Options { get; } = new ModalOptions() { Title = "Add node" };

        public ICustomCommand SelectCommand { get; }
        public ScopedElement? Selected { get; set; }
        public Scope? CurrentScope { get; set; }

        private readonly ScopesClient _client;

        public ScopedSelectorModal()
        {
            _client = Services.Provider.GetRequiredService<ScopesClient>();
            this.Loaded += ScopedSelectorModal_Loaded;

            SelectCommand = new DelegateCommand(
                () => ParentLayout?.Hide(true),
                () => Selected != null &&
                    (Selected.Metadata.Type == Automation.Shared.Data.EnumScopedType.Workflow ||
                        Selected.Metadata.Type == Automation.Shared.Data.EnumScopedType.Task));

            InitializeComponent();
        }

        private async void ScopedSelectorModal_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentScope = await _client.GetRootAsync();
            CurrentScope.Refresh();
        }

        private async void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Selected is not Scope scope)
                return;
            CurrentScope = await _client.GetByIdAsync(scope.Id);
            CurrentScope!.Refresh();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectCommand.RaiseCanExecuteChanged();
        }

        private async void BreadcrumbScopeChanged(Scope scope)
        {
            CurrentScope = await _client.GetByIdAsync(scope.Id);
            CurrentScope!.Refresh();
        }
    }
}
