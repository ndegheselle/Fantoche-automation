using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Data;

/// <summary>
/// JSON schemas as a graph stores them : text, parsed on demand. And the values of the tasks, written
/// the way the schemas of their types describe them.
/// </summary>
public static class Schemas
{
    // The enums are written by name in the schemas and in the values alike : what a mapping or a
    // page writes, and how the plugins declare theirs (JsonStringEnumConverter). The schemas are
    // generated from System.Text.Json while the values are written by Newtonsoft, which writes
    // enums as numbers unless told otherwise.
    private static readonly SystemTextJsonSchemaGeneratorSettings GeneratorSettings = new()
    {
        SerializerOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } },
    };

    private static readonly Newtonsoft.Json.JsonSerializer ValueSerializer = Newtonsoft.Json.JsonSerializer.Create(
        new Newtonsoft.Json.JsonSerializerSettings { Converters = { new StringEnumConverter() } });

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

    /// <summary>The schema of the parameters or the output of a task, of [type].</summary>
    public static JsonSchema Of(Type type) => JsonSchema.FromType(type, GeneratorSettings);

    /// <summary>
    /// [value], handed over by a task or to a page, as JSON fitting the schema of its type (see
    /// <see cref="Of"/>).
    /// </summary>
    public static JToken ValueOf(object value) => JToken.FromObject(value, ValueSerializer);
}
