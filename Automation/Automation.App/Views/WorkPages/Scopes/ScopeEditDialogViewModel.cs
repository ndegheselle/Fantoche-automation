using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// MIGRATION: replaces ScopeEditModal. ShadUI dialog content to edit a scope's metadata
    /// (via MetadataEdit) and save through <see cref="IScopesService"/>. (Scope creation reuses the
    /// generic TextBoxDialog for the name + IScopesService.CreateAsync; see ScopedContextMenu.)
    /// </summary>
    public partial class ScopeEditDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly IScopesService _scopes;

        public Scope Scope { get; }

        /// <summary>Bound by the MetadataEdit control.</summary>
        public ScopedMetadata Metadata => Scope.Metadata;

        public ScopeEditDialogViewModel(DialogManager dialogManager, ToastManager toastManager, IScopesService scopes, Scope scope)
        {
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _scopes = scopes;
            Scope = scope;
        }

        [RelayCommand]
        private async Task Save()
        {
            try
            {
                await _scopes.UpdateAsync(Scope.Id, Scope);
                _toastManager.CreateToast("Scope updated").DismissOnClick().ShowSuccess();
                _dialogManager.Close(this, new CloseDialogOptions { Success = true });
            }
            catch (ValidationException ex)
            {
                // TODO: surface per-field errors in MetadataEdit (the current domain Scope has no
                // error model, unlike the drifted one). For now, report the validation summary.
                string message = ex.Errors == null
                    ? "Validation failed."
                    : string.Join("\n", ex.Errors.SelectMany(e => e.Value));
                _toastManager.CreateToast("Invalid scope").WithContent(message).DismissOnClick().ShowError();
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Saving is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
