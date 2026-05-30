using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// MIGRATION: replaces the Joufflu FilePickerModal (Joufflu.Inputs.FilePicker). The actual file
    /// browse uses Avalonia's StorageProvider from the view's code-behind (it needs a TopLevel).
    /// Result: <see cref="FilePath"/>.
    /// </summary>
    public partial class FilePickerDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _subTitle;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private string? _filePath;

        public FilePickerDialogViewModel(DialogManager dialogManager, string title, string subTitle = "")
        {
            _dialogManager = dialogManager;
            _title = title;
            _subTitle = subTitle;
        }

        private bool CanValidate() => !string.IsNullOrEmpty(FilePath);

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
