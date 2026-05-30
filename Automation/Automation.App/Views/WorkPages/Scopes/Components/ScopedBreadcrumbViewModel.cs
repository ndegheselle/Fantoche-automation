using System.Collections.ObjectModel;
using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// MIGRATION (vertical slice): breadcrumb of a scope's ancestor chain. Replaces the old
    /// code-behind that called ScopesClient directly; now depends on <see cref="IScopesService"/>
    /// and current Automation.Shared.Data.Scoped models. The view binds to this view-model.
    /// </summary>
    public partial class ScopedBreadcrumbViewModel : ObservableObject
    {
        private readonly IScopesService _scopes;

        /// <summary>Ancestor scopes (root-first) followed by the current scope itself.</summary>
        public ObservableCollection<Scope> Parents { get; } = [];

        /// <summary>Raised when the user picks a different scope in the breadcrumb.</summary>
        public event Action<Scope>? ScopeSelected;

        private Scope? _current;

        public ScopedBreadcrumbViewModel(IScopesService scopes)
        {
            _scopes = scopes;
        }

        public async Task LoadAsync(Scope? scope)
        {
            _current = scope;
            Parents.Clear();

            if (scope == null || scope.Id == Scope.ROOT_SCOPE_ID)
                return;

            try
            {
                foreach (Scope parent in await _scopes.GetParentScopesAsync(scope.Id))
                    Parents.Add(parent);

                // The current scope is shown as the last breadcrumb segment.
                Parents.Add(scope);
            }
            catch (NotImplementedException)
            {
                // Data source not wired yet (migration stub). Render an empty breadcrumb.
            }
        }

        public void Select(Scope? target)
        {
            if (target == null || target == _current)
                return;

            ScopeSelected?.Invoke(target);
        }
    }
}
