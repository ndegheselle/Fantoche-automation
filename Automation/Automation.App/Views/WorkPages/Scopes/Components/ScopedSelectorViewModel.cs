using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// MIGRATION: scope navigator. Replaces the old code-behind that called ScopesClient directly +
    /// used ILoading. Loads the root scope and navigates into sub-scopes via <see cref="IScopesService"/>,
    /// exposing the current scope's children (wrapped as <see cref="ScopedElementViewModel"/>) and a
    /// busy flag (bound to a ShadUI BusyArea).
    /// </summary>
    public partial class ScopedSelectorViewModel : ObservableObject
    {
        private readonly IScopesService _scopes;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Current))]
        private ScopeViewModel? _currentScope;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Current))]
        private ScopedElementViewModel? _selected;

        [ObservableProperty]
        private bool _isBusy;

        /// <summary>The selected element, or the current scope itself when nothing is selected.</summary>
        public ScopedElementViewModel? Current => Selected ?? CurrentScope;

        public event Action<ScopedElementViewModel?>? SelectionChanged;

        public ScopedSelectorViewModel(IScopesService scopes) => _scopes = scopes;

        partial void OnSelectedChanged(ScopedElementViewModel? value) => SelectionChanged?.Invoke(value);

        public async Task LoadRootAsync()
        {
            await LoadAsync(() => _scopes.GetRootAsync()!);
        }

        public async Task OpenAsync(ScopeViewModel scope)
        {
            await LoadAsync(async () => await _scopes.GetByIdAsync(scope.Model.Id));
        }

        private async Task LoadAsync(Func<Task<Automation.Shared.Data.Scoped.Scope?>> load)
        {
            try
            {
                IsBusy = true;
                var scope = await load();
                if (scope != null)
                {
                    Selected = null;
                    CurrentScope = new ScopeViewModel(scope);
                }
            }
            catch (NotImplementedException)
            {
                // Data source not wired yet (migration stub).
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
