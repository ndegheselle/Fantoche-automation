using System.ComponentModel;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Media;
using Lucide.Avalonia;

namespace Automation.App.Views.WorkPages.Components
{
    /// <summary>
    /// MIGRATION (WPF -> Avalonia): was a Joufflu icon-font TextBlock whose Text was a Phosphor
    /// glyph. Now derives from Lucide.Avalonia's LucideIcon and drives its Kind (+ Foreground)
    /// from the bound ScopedMetadata. Stored Metadata.Icon (dev/test data only) is interpreted as
    /// a LucideIconKind name; otherwise the icon defaults by scoped type.
    /// </summary>
    internal class ScopedIcon : LucideIcon
    {
        public static readonly StyledProperty<ScopedMetadata?> MetadataProperty =
            AvaloniaProperty.Register<ScopedIcon, ScopedMetadata?>(nameof(Metadata));

        public ScopedMetadata? Metadata
        {
            get => GetValue(MetadataProperty);
            set => SetValue(MetadataProperty, value);
        }

        private ScopedMetadata? _previousMetadata;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == MetadataProperty)
                OnMetadataChanged();
        }

        private void OnMetadataChanged()
        {
            if (_previousMetadata != null)
                _previousMetadata.PropertyChanged -= Metadata_PropertyChanged;

            if (Metadata == null)
                return;

            Metadata.PropertyChanged += Metadata_PropertyChanged;

            Kind = ResolveKind(Metadata);

            if (!string.IsNullOrEmpty(Metadata.Color) && Color.TryParse(Metadata.Color, out var color))
                Foreground = new SolidColorBrush(color);

            _previousMetadata = Metadata;
        }

        private static LucideIconKind ResolveKind(ScopedMetadata metadata)
        {
            if (!string.IsNullOrEmpty(metadata.Icon) && Enum.TryParse<LucideIconKind>(metadata.Icon, out var stored))
                return stored;

            return metadata.Type switch
            {
                EnumScopedType.Scope => LucideIconKind.Folder,
                EnumScopedType.Workflow => LucideIconKind.Network,
                _ => LucideIconKind.Waypoints,
            };
        }

        private void Metadata_PropertyChanged(object? sender, PropertyChangedEventArgs e) => OnMetadataChanged();
    }
}
