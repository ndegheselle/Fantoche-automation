using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// MIGRATION: replaces PackageDetailModal. Shows a package's versions and the task targets
    /// (classes) of the selected version, and returns the chosen <see cref="ClassTarget"/>.
    /// GetClassesAsync now returns TaskTarget (the old ClassIdentifier replacement).
    /// </summary>
    public partial class PackageDetailDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly IPackagesService _packages;

        public PackageInfos Package { get; }

        [ObservableProperty]
        private IReadOnlyList<Version> _versions = [];

        [ObservableProperty]
        private Version? _selectedVersion;

        [ObservableProperty]
        private IReadOnlyList<ClassTarget> _classes = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
        private ClassTarget? _selectedClass;

        public PackageDetailDialogViewModel(DialogManager dialogManager, ToastManager toastManager, IPackagesService packages, PackageInfos package)
        {
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _packages = packages;
            Package = package;
            _selectedVersion = package.Identifier.Version;
            _ = LoadVersionsAsync();
        }

        partial void OnSelectedVersionChanged(Version? value) => _ = LoadClassesAsync();

        private async Task LoadVersionsAsync()
        {
            try
            {
                Versions = await _packages.GetVersionsAsync(Package.Identifier.Identifier);
                SelectedVersion = Versions.FirstOrDefault() ?? SelectedVersion;
            }
            catch (NotImplementedException) { Versions = []; }
        }

        private async Task LoadClassesAsync()
        {
            if (SelectedVersion is null)
                return;

            try
            {
                var targets = await _packages.GetClassesAsync(Package.Identifier.Identifier, SelectedVersion);
                Classes = targets.OfType<ClassTarget>().ToList();
            }
            catch (NotImplementedException) { Classes = []; }
        }

        private bool CanSelect() => SelectedClass != null;

        [RelayCommand(CanExecute = nameof(CanSelect))]
        private void Select() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);

        [RelayCommand]
        private void DeleteVersion()
        {
            if (SelectedVersion is not { } version)
                return;

            _dialogManager.CreateDialog("Delete version", $"Remove version '{version}' of this package?")
                .WithPrimaryButton("Delete", () => _ = RemoveVersionAsync(version), DialogButtonStyle.Destructive)
                .WithCancelButton("Cancel")
                .Dismissible()
                .Show();
        }

        private async Task RemoveVersionAsync(Version version)
        {
            try
            {
                await _packages.RemoveVersionAsync(Package.Identifier.Identifier, version);
                await LoadVersionsAsync();
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Removing is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }
    }
}
