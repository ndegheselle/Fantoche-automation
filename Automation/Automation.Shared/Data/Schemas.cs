using NJsonSchema;

namespace Automation.Shared.Data;

/// <summary>
/// JSON schemas as a graph stores them : text, parsed on demand.
/// </summary>
internal static class Schemas
{
    /// <summary>
    /// [json] as a schema, null when it holds none.
    /// <para>
    /// Parsing one costs about two orders of magnitude more than anything else walking a graph does
    /// — some 0.06 ms against 0.0008 ms for parsing a mapping — and a walk reads the schema of every
    /// node it passes. Which is why everything holding one keeps what it parsed and drops it when
    /// the text changes, rather than parsing it again on every read.
    /// </para>
    /// </summary>
    public static JsonSchema? Parse(string? json) => json == null ? null : JsonSchema.FromJsonAsync(json).Result;
}
