using Automation.App.ViewModels.Scoped;
using Automation.Shared.Data.Scoped;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.WorkPages
{
    public partial class TasksMainPage : UserControl
    {
        public TasksMainPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private TasksMainPageViewModel? ViewModel => DataContext as TasksMainPageViewModel;

        private void Selector_SelectionChanged(ScopedElementViewModel? element)
        {
            // Selecting nothing falls back to the current scope's page.
            ViewModel?.ShowElement(element ?? Selector.CurrentScope);
        }

        private void Selector_CurrentScopeChanged(ScopeViewModel? scope)
        {
            if (ViewModel is null)
                return;

            ViewModel.CurrentScope = scope?.Model;
            // On navigation/root load with no element selected, show the scope's page.
            if (Selector.Current is null or ScopeViewModel)
                ViewModel.ShowElement(scope);
        }

        private async void Breadcrumb_ScopeSelected(Scope scope)
        {
            await Selector.OpenScopeAsync(scope);
        }
    }
}
