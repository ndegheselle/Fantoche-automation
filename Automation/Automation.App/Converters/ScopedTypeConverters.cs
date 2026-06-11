using System.Collections.Generic;
using Automation.App.Assets.Fonts;
using Automation.Shared.Data.Scoped;
using Avalonia.Data.Converters;

namespace Automation.App.Converters;

public static class ScopedTypeConverters
{
    public static readonly Dictionary<EnumScopedType, string> Icons = new()
    {
        { EnumScopedType.Scope, LucideIconFont.Folder },
        { EnumScopedType.Workflow, LucideIconFont.StickyNotes },
        { EnumScopedType.Task, LucideIconFont.StickyNote }
    };

    public static readonly IValueConverter ToLucideIcon =
        new FuncValueConverter<EnumScopedType, string>(type => Icons.TryGetValue(type, out var icon) ? icon : string.Empty);

    /// <summary>
    /// Resolves the glyph to display for a metadata: the custom <see cref="ScopedMetadata.Icon"/>
    /// when set, otherwise the default icon for its <see cref="ScopedMetadata.Type"/>.
    /// </summary>
    public static readonly IValueConverter ToIcon =
        new FuncValueConverter<ScopedMetadata, string>(metadata =>
        {
            if (metadata is null)
                return string.Empty;
            if (!string.IsNullOrEmpty(metadata.Icon))
                return metadata.Icon;
            return Icons.TryGetValue(metadata.Type, out var icon) ? icon : string.Empty;
        });
}
