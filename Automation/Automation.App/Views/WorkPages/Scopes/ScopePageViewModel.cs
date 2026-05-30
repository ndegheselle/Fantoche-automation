using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// MIGRATION: content shown when a scope is opened (replaces ScopePage : IPage). Resolved to the
    /// ScopePage view by the ViewLocator. Hosts the execution-history tab and a settings action that
    /// opens the ScopeEditDialog.
    /// </summary>
    public partial class ScopePageViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly IScopesService _scopes;

        public ScopeViewModel Scope { get; }

        public ScopePageViewModel(ScopeViewModel scope, DialogManager dialogManager, ToastManager toastManager, IScopesService scopes)
        {
            Scope = scope;
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _scopes = scopes;
        }

        [RelayCommand]
        public void OpenSettings()
        {
            var vm = new ScopeEditDialogViewModel(_dialogManager, _toastManager, _scopes, Scope.Model);
            _dialogManager.CreateDialog(vm).Dismissible().Show();
        }
    }
}
