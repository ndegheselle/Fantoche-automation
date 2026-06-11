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

    /// <summary>
    /// Resolves the glyph to display: accepts either an <see cref="EnumScopedType"/> (returns the
    /// default icon) or a <see cref="ScopedMetadata"/> (returns the custom icon when set, otherwise
    /// the default icon for its type).
    /// </summary>
    public static readonly IValueConverter ToIcon =
        new FuncValueConverter<object, string>(input => input switch
        {
            ScopedMetadata { Icon: { Length: > 0 } icon } => icon,
            ScopedMetadata metadata => Icons.TryGetValue(metadata.Type, out var icon) ? icon : string.Empty,
            EnumScopedType type => Icons.TryGetValue(type, out var icon) ? icon : string.Empty,
            _ => string.Empty
        });
}
