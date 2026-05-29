using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

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

        public ScopedElementViewModel? Current => _viewModel.Current;

        public ScopedSelector()
        {
            _viewModel = new ScopedSelectorViewModel(AppServices.Provider.GetRequiredService<IScopesService>());
            _viewModel.SelectionChanged += element => SelectionChanged?.Invoke(element);
            DataContext = _viewModel;
            InitializeComponent();
            Loaded += async (_, _) => await _viewModel.LoadRootAsync();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_viewModel.Selected is ScopeViewModel scope)
                await _viewModel.OpenAsync(scope);
        }
    }
}
