using System.Collections.Generic;
using System.Reflection;

namespace Automation.App.Assets.Fonts;

/// <summary>
/// A single Lucide icon: its field <see cref="Name"/> and the font <see cref="Glyph"/> to render.
/// </summary>
public sealed record LucideIconItem(string Name, string Glyph);

/// <summary>
/// Enumerates every icon declared in <see cref="LucideIconFont"/> using reflection so they can be
/// presented in a picker. The catalog is built once and cached.
/// </summary>
public static class LucideIconCatalog
{
    private static IReadOnlyList<LucideIconItem>? _all;

    public static IReadOnlyList<LucideIconItem> All => _all ??= Load();

    private static IReadOnlyList<LucideIconItem> Load()
    {
        var icons = new List<LucideIconItem>();
        FieldInfo[] fields = typeof(LucideIconFont)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var field in fields)
        {
            // The font is generated as 'const string' fields, one per icon.
            if (!field.IsLiteral || field.FieldType != typeof(string))
                continue;

            if (field.GetRawConstantValue() is string glyph && !string.IsNullOrEmpty(glyph))
                icons.Add(new LucideIconItem(field.Name, glyph));
        }

        icons.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return icons;
    }
}
