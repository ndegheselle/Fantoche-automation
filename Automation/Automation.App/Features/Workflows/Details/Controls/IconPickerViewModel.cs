using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Assets.Fonts;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// An icon, as selectable in the <see cref="IconPicker"/> : its Lucide glyph name (for search) and
    /// its glyph character (for display and storage).
    /// </summary>
    public record IconEntry(string Name, string Glyph);

    /// <summary>
    /// Search state backing an <see cref="IconPicker"/>'s popup, listing the Lucide glyphs exposed by
    /// <see cref="LucideFontIcons"/>.
    /// </summary>
    public partial class IconPickerViewModel : ObservableObject
    {
        /// <summary>
        /// Caps the number of icons rendered at once, the full icon set being too large to display
        /// without filtering.
        /// </summary>
        private const int MaxResults = 60;

        private static readonly IReadOnlyList<IconEntry> AllIcons = typeof(LucideFontIcons)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => new IconEntry(field.Name, (string)field.GetRawConstantValue()!))
            // Icons whose name isn't a valid identifier are prefixed with an underscore : they are
            // pushed last so the unfiltered grid opens on the named ones.
            .OrderBy(icon => icon.Name.StartsWith('_'))
            .ThenBy(icon => icon.Name)
            .ToList();

        [ObservableProperty]
        private string _searchText = "";

        public ObservableCollection<IconEntry> FilteredIcons { get; } = [];

        /// <summary>
        /// Raised with the picked glyph when the user selects an icon.
        /// </summary>
        public event Action<string>? IconPicked;

        public IconPickerViewModel()
        {
            Refresh();
        }

        partial void OnSearchTextChanged(string value) => Refresh();

        private void Refresh()
        {
            IEnumerable<IconEntry> matches = string.IsNullOrWhiteSpace(SearchText)
                ? AllIcons
                : AllIcons.Where(icon => icon.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            FilteredIcons.Clear();
            foreach (IconEntry icon in matches.Take(MaxResults))
                FilteredIcons.Add(icon);
        }

        [RelayCommand]
        private void Select(string glyph) => IconPicked?.Invoke(glyph);
    }
}
