using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// MIGRATION: replaces the Joufflu IModalContent TextBoxModal. Content view-model for a ShadUI
    /// dialog that prompts for a single text value. Opened by a caller via:
    /// <code>
    /// var vm = new TextBoxDialogViewModel(dialogManager, "Title", "Prompt");
    /// dialogManager.CreateDialog(vm).Dismissible()
    ///     .WithSuccessCallback(v => use v.Value)
    ///     .WithCancelCallback(() => { })
    ///     .Show();
    /// </code>
    /// </summary>
    public partial class TextBoxDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _subTitle;

        [ObservableProperty]
        private string? _value;

        public TextBoxDialogViewModel(DialogManager dialogManager, string title, string subTitle = "", string? value = null)
        {
            _dialogManager = dialogManager;
            _title = title;
            _subTitle = subTitle;
            _value = value;
        }

        [RelayCommand]
        private void Validate() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
