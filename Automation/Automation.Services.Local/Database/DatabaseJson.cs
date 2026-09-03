using System.Text.Json;

namespace Automation.Services.Local.Database;

/// <summary>
/// What is stored as JSON rather than as columns of its own : the settings of an element, its
/// schedules, the tags of a metadata. Written with the defaults of the framework, the types
/// carrying whatever they need of their own (the discriminator of a derived type, a version).
/// </summary>
internal static class DatabaseJson
{
    public static string? Serialize<T>(T? value) where T : class
    {
        return value == null ? null : JsonSerializer.Serialize(value);
    }

    public static T? Deserialize<T>(string? json) where T : class
    {
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<T>(json);
    }
}
