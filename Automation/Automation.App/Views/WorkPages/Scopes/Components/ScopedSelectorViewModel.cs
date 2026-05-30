using Automation.App.Components.Inputs;
using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// MIGRATION: scope navigator + context actions. Replaces the old code-behind (ScopesClient,
    /// ILoading) and the separate ScopedContextMenu control. Navigation via <see cref="IScopesService"/>;
    /// add/remove via the services + ShadUI dialogs (TextBoxDialog for the name, a destructive
    /// confirm for remove). Children are <see cref="ScopedElementViewModel"/> wrappers.
    /// </summary>
    public partial class ScopedSelectorViewModel : ObservableObject
    {
        private readonly IScopesService _scopes;
        private readonly ITasksService _tasks;
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Current), nameof(CanAddChild))]
        [NotifyCanExecuteChangedFor(nameof(AddScopeCommand), nameof(AddTaskCommand), nameof(AddWorkflowCommand))]
        private ScopeViewModel? _currentScope;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Current), nameof(CanAddChild))]
        [NotifyCanExecuteChangedFor(nameof(AddScopeCommand), nameof(AddTaskCommand), nameof(AddWorkflowCommand), nameof(RemoveSelectedCommand))]
        private ScopedElementViewModel? _selected;

        [ObservableProperty]
        private bool _isBusy;

        /// <summary>The selected element, or the current scope itself when nothing is selected.</summary>
        public ScopedElementViewModel? Current => Selected ?? CurrentScope;

        /// <summary>Children can be added when the active element is a scope.</summary>
        public bool CanAddChild => Current is ScopeViewModel;

        public event Action<ScopedElementViewModel?>? SelectionChanged;

        public ScopedSelectorViewModel(IScopesService scopes, ITasksService tasks, DialogManager dialogManager, ToastManager toastManager)
        {
            _scopes = scopes;
            _tasks = tasks;
            _dialogManager = dialogManager;
            _toastManager = toastManager;
        }

        partial void OnSelectedChanged(ScopedElementViewModel? value) => SelectionChanged?.Invoke(value);

        #region Navigation

        public Task LoadRootAsync() => LoadAsync(() => _scopes.GetRootAsync()!);

        public Task OpenAsync(ScopeViewModel scope) => LoadAsync(() => _scopes.GetByIdAsync(scope.Model.Id));

        private async Task LoadAsync(Func<Task<Scope?>> load)
        {
            try
            {
                IsBusy = true;
                Scope? scope = await load();
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

        #endregion

        #region Add / remove

        [RelayCommand(CanExecute = nameof(CanAddChild))]
        private void AddScope() => PromptCreate("Create scope", new Scope(), element => _scopes.CreateAsync(element));

        [RelayCommand(CanExecute = nameof(CanAddChild))]
        private void AddTask() => PromptCreate("Create task", new AutomationTask(), element => _tasks.CreateAsync((BaseAutomationTask)element));

        [RelayCommand(CanExecute = nameof(CanAddChild))]
        private void AddWorkflow() => PromptCreate("Create workflow", new AutomationWorkflow(), element => _tasks.CreateAsync((BaseAutomationTask)element));

        private void PromptCreate(string title, ScopedElement element, Func<ScopedElement, Task<Guid>> create)
        {
            if (Current is not ScopeViewModel parent)
                return;

            var nameVm = new TextBoxDialogViewModel(_dialogManager, title, "Name");
            _dialogManager.CreateDialog(nameVm)
                .Dismissible()
                .WithSuccessCallback(vm => { _ = CreateAsync(parent, element, nameVm.Value, create); })
                .Show();
        }

        private async Task CreateAsync(ScopeViewModel parent, ScopedElement element, string? name, Func<ScopedElement, Task<Guid>> create)
        {
            element.Metadata.Name = name ?? string.Empty;
            element.ChangeParent(parent.Model);
            try
            {
                element.Id = await create(element);
                ScopedElementViewModel vm = parent.AddChild(element);
                vm.FocusOn = EnumScopedTab.Settings;
                Selected = vm;
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Creating is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }

        public bool CanRemove => Selected is { } selected && selected.Model.Parent != null;

        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void RemoveSelected()
        {
            if (Selected is not { } target)
                return;

            _dialogManager.CreateDialog("Remove element",
                    $"Are you sure you want to remove '{target.Metadata.Name}'? This cannot be undone.")
                .WithPrimaryButton("Remove", () => _ = RemoveAsync(target), DialogButtonStyle.Destructive)
                .WithCancelButton("Cancel")
                .Dismissible()
                .Show();
        }

        private async Task RemoveAsync(ScopedElementViewModel target)
        {
            try
            {
                if (target.Type == EnumScopedType.Scope)
                    await _scopes.DeleteAsync(target.Id);
                else
                    await _tasks.DeleteAsync(target.Id);

                CurrentScope?.RemoveChild(target);
                Selected = null;
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Removing is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }

        #endregion
    }
}
