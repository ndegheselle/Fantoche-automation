using System.Collections.ObjectModel;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows
{
    /// <summary>
    /// One entry of the navigation breadcrumb.
    /// </summary>
    public class ScopeCrumb
    {
        public Guid Id { get; }
        public string Name { get; }

        public ScopeCrumb(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// Explorer over the scoped elements (scopes, workflows and tasks). Starts at the root scope
    /// and lets the user drill down into scopes through a breadcrumb.
    /// </summary>
    public class WorkflowsViewModel : ObservableObject
    {
        private readonly IScopedService _scoped;

        public ObservableCollection<ScopedElement> Elements { get; } = [];
        public ObservableCollection<ScopeCrumb> Breadcrumb { get; } = [];

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

        public IAsyncRelayCommand<ScopedElement> OpenCommand { get; }
        public IAsyncRelayCommand<ScopeCrumb> NavigateCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        public WorkflowsViewModel(IScopedService scoped)
        {
            _scoped = scoped;

            OpenCommand = new AsyncRelayCommand<ScopedElement>(OpenAsync, CanOpen);
            NavigateCommand = new AsyncRelayCommand<ScopeCrumb>(NavigateAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);

            Breadcrumb.Add(new ScopeCrumb(Scope.ROOT_SCOPE_ID, "Automation"));
            _ = LoadAsync(Scope.ROOT_SCOPE_ID);
        }

        private static bool CanOpen(ScopedElement? element) => element is Scope;

        private async Task OpenAsync(ScopedElement? element)
        {
            if (element is not Scope)
                return;

            Breadcrumb.Add(new ScopeCrumb(element.Id, element.Metadata.Name));
            await LoadAsync(element.Id);
        }

        private async Task NavigateAsync(ScopeCrumb? crumb)
        {
            if (crumb == null)
                return;

            // Drop every crumb after the clicked one.
            int index = Breadcrumb.IndexOf(crumb);
            if (index < 0)
                return;

            for (int i = Breadcrumb.Count - 1; i > index; i--)
                Breadcrumb.RemoveAt(i);

            await LoadAsync(crumb.Id);
        }

        private Task RefreshAsync()
        {
            var current = Breadcrumb.LastOrDefault();
            return LoadAsync(current?.Id ?? Scope.ROOT_SCOPE_ID);
        }

        private async Task LoadAsync(Guid scopeId)
        {
            IsLoading = true;
            try
            {
                var children = await _scoped.GetChildrens(scopeId);
                Elements.Clear();
                // Scopes first, then the rest, alphabetically.
                foreach (var element in children
                             .OrderByDescending(x => x is Scope)
                             .ThenBy(x => x.Metadata.Name))
                    Elements.Add(element);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
