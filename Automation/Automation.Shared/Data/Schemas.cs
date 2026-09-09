using NJsonSchema;

namespace Automation.Shared.Data;

/// <summary>
/// JSON schemas as a graph stores them : text, parsed on demand.
/// </summary>
public static class Schemas
{
    /// <summary>
    /// [json] as a schema, null when it holds none. Throws when it holds something that isn't one,
    /// whoever is displaying the text being where that is worth reporting.
    /// <para>
    /// Blocking on the parse : reading a schema is what a graph walk does inline, and every caller
    /// of this is on a thread that has nothing else to do meanwhile.
    /// </para>
    /// <para>
    /// Parsing one costs about two orders of magnitude more than anything else walking a graph does
    /// — some 0.06 ms against 0.0008 ms for parsing a mapping — and a walk reads the schema of every
    /// node it passes. Which is why everything holding one keeps what it parsed and drops it when
    /// the text changes, rather than parsing it again on every read.
    /// </para>
    /// </summary>
    public static JsonSchema? Parse(string? json) => json == null ? null : JsonSchema.FromJsonAsync(json).Result;
}
