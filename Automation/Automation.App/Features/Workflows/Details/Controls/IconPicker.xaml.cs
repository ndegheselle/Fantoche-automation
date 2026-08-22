using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// A button showing the currently selected Lucide glyph, opening a searchable popup of every
    /// available icon to pick from.
    /// </summary>
    public partial class IconPicker : UserControl
    {
        public static readonly DependencyProperty SelectedIconProperty = DependencyProperty.Register(
            nameof(SelectedIcon), typeof(string), typeof(IconPicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? SelectedIcon
        {
            get => (string?)GetValue(SelectedIconProperty);
            set => SetValue(SelectedIconProperty, value);
        }

        /// <summary>
        /// Search state of the popup, kept separate from <see cref="SelectedIcon"/> so the control can
        /// be used against any DataContext.
        /// </summary>
        public IconPickerViewModel Icons { get; } = new();

        public IconPicker()
        {
            InitializeComponent();
            // Set directly rather than bound from XAML : the popup content is a separate NameScope,
            // so an ElementName binding back to this control silently fails to resolve there.
            PopupContent.DataContext = Icons;
            Icons.IconPicked += icon => SelectedIcon = icon;
        }
    }
}
