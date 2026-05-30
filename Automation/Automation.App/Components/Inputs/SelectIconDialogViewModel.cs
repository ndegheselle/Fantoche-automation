using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;
using ShadUI;

namespace Automation.App.Components.Inputs
{
    public class IconItem
    {
        public LucideIconKind Kind { get; init; }
        public string Name { get; init; } = "";
    }

    /// <summary>
    /// MIGRATION: replaces the Joufflu SelectIconModal (Phosphor glyphs). Lists Lucide icons with
    /// a search filter. Result: <see cref="Selected"/> (its Kind name is stored in Metadata.Icon,
    /// matching how ScopedIcon parses it).
    /// </summary>
    public partial class SelectIconDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly List<IconItem> _allIcons;

        public ObservableCollection<IconItem> Icons { get; } = [];

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private IconItem? _selected;

        public SelectIconDialogViewModel(DialogManager dialogManager, string? initialIcon = null)
        {
            _dialogManager = dialogManager;
            _allIcons = Enum.GetValues<LucideIconKind>()
                .Select(kind => new IconItem { Kind = kind, Name = kind.ToString() })
                .ToList();

            if (!string.IsNullOrEmpty(initialIcon) && Enum.TryParse<LucideIconKind>(initialIcon, out var k))
                _selected = _allIcons.FirstOrDefault(i => i.Kind == k);

            Filter();
        }

        partial void OnSearchTextChanged(string value) => Filter();

        private void Filter()
        {
            IEnumerable<IconItem> matches = string.IsNullOrWhiteSpace(SearchText)
                ? _allIcons
                : _allIcons.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            Icons.Clear();
            foreach (IconItem item in matches)
                Icons.Add(item);
        }

        private bool CanValidate() => Selected != null;

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
