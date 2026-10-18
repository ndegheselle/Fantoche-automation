using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Tables;

/// <summary>
/// What a row reference (see <see cref="RowScope"/>) points at, read the same way by every table task.
/// </summary>
internal static class RowReference
{
    public const string Index = $"{RowScope.Root}.index";
    private const string Columns = $"{RowScope.Root}.columns";

    /// <summary>
    /// The column [reference] points at, as "$row.columns.name" or "$row.columns['unit price']".
    /// Only called on references other than <see cref="Index"/>.
    /// </summary>
    public static string ColumnOf(string reference)
    {
        string path = reference.StartsWith(Columns) ? reference[Columns.Length..] : "";
        if (path.Length > 1 && path.StartsWith('.'))
            return path[1..];
        if (path.Length > 4 && path.StartsWith("['") && path.EndsWith("']"))
            return path[2..^2];

        throw new ArgumentException($"[{reference}] is not a row reference, expected \"{Index}\" or \"{Columns}.<name>\".");
    }
}
