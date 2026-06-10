using System.Collections.Generic;
using Automation.App.Assets.Fonts;
using Automation.Shared.Data.Scoped;
using Avalonia.Data.Converters;

namespace Automation.App.Converters;

public static class ScopedTypeConverters
{
    private static readonly Dictionary<EnumScopedType, string> Icons = new()
    {
        { EnumScopedType.Scope, LucideIconFont.Folder },
        { EnumScopedType.Workflow, LucideIconFont.StickyNotes },
        { EnumScopedType.Task, LucideIconFont.StickyNote }
    };

    public static readonly IValueConverter ToLucideIcon =
        new FuncValueConverter<EnumScopedType, string>(type => Icons.TryGetValue(type, out var icon) ? icon : string.Empty);
}
