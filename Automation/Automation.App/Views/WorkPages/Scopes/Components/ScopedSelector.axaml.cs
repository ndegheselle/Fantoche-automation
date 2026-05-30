using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using Automation.Shared.Data.Scoped;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Scope navigator control. Loads the root scope on attach; double-clicking a sub-scope
    /// navigates into it. Raises <see cref="SelectionChanged"/> for hosting pages.
    /// </summary>
    public partial class ScopedSelector : UserControl
    {
        private readonly ScopedSelectorViewModel _viewModel;

        /// <summary>Raised when the selected element changes.</summary>
        public event Action<ScopedElementViewModel?>? SelectionChanged;

        /// <summary>Raised when the displayed (current) scope changes (root load / navigation).</summary>
        public event Action<ScopeViewModel?>? CurrentScopeChanged;

        public ScopedElementViewModel? Current => _viewModel.Current;

        public ScopeViewModel? CurrentScope => _viewModel.CurrentScope;

        public ScopedSelector()
        {
            IServiceProvider provider = AppServices.Provider;
            _viewModel = new ScopedSelectorViewModel(
                provider.GetRequiredService<IScopesService>(),
                provider.GetRequiredService<ITasksService>(),
                provider.GetRequiredService<DialogManager>(),
                provider.GetRequiredService<ToastManager>());
            _viewModel.SelectionChanged += element => SelectionChanged?.Invoke(element);
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScopedSelectorViewModel.CurrentScope))
                    CurrentScopeChanged?.Invoke(_viewModel.CurrentScope);
            };
            DataContext = _viewModel;
            InitializeComponent();
            Loaded += async (_, _) => await _viewModel.LoadRootAsync();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        /// <summary>Navigates the selector into the given scope (used by the breadcrumb).</summary>
        public Task OpenScopeAsync(Scope scope) => _viewModel.OpenAsync(new ScopeViewModel(scope));

        private async void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_viewModel.Selected is ScopeViewModel scope)
                await _viewModel.OpenAsync(scope);
        }
    }
}
