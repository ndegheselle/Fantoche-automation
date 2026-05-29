using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Components.Inputs
{
    /// <summary>
    /// MIGRATION: replaces the Joufflu SelectColorModal (a fixed palette). Uses Avalonia's
    /// ColorPicker (themed by ShadUI). Result: <see cref="Hex"/> (e.g. "#1982C4").
    /// </summary>
    public partial class SelectColorDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;

        [ObservableProperty]
        private Color _selectedColor;

        public string Hex => $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";

        public SelectColorDialogViewModel(DialogManager dialogManager, string? initialColor = null)
        {
            _dialogManager = dialogManager;
            SelectedColor = !string.IsNullOrEmpty(initialColor) && Color.TryParse(initialColor, out var color)
                ? color
                : Colors.SteelBlue;
        }

        [RelayCommand]
        private void Validate() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
