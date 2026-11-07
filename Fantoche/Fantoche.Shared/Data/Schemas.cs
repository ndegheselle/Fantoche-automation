using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
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
        // [Description] only: the <summary> are written for developers, not for the editor's fields.
        UseXmlDocumentation = false,
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

    /// <summary>
    /// [error] as the user reads it : the value it is about (its JSON path, as
    /// <see cref="JToken.Path"/> writes it), then what is expected of it.
    /// </summary>
    public static string Describe(ValidationError error)
    {
        string path = PathOf(error);
        string place = path.Length > 0 ? path : "value";
        return $"{place} : {Expectation(error)}";
    }

    /// <summary>The path of the value [error] is about, as <see cref="JToken.Path"/> writes it.</summary>
    public static string PathOf(ValidationError error) => error.Path?.TrimStart('#', '/') ?? "";

    private static string Expectation(ValidationError error)
    {
        JsonSchema? schema = error.Schema;
        return error.Kind switch
        {
            ValidationErrorKind.PropertyRequired => "required",
            // The exclusive limit is a number of its own since draft 6, a flag on the limit before.
            ValidationErrorKind.NumberTooSmall when schema?.ExclusiveMinimum != null => Invariant($"more than {schema.ExclusiveMinimum}"),
            ValidationErrorKind.NumberTooSmall when schema?.IsExclusiveMinimum == true => Invariant($"more than {schema.Minimum}"),
            ValidationErrorKind.NumberTooSmall => Invariant($"at least {schema?.Minimum}"),
            ValidationErrorKind.NumberTooBig when schema?.ExclusiveMaximum != null => Invariant($"less than {schema.ExclusiveMaximum}"),
            ValidationErrorKind.NumberTooBig when schema?.IsExclusiveMaximum == true => Invariant($"less than {schema.Maximum}"),
            ValidationErrorKind.NumberTooBig => Invariant($"at most {schema?.Maximum}"),
            ValidationErrorKind.StringTooShort => $"at least {schema?.MinLength} characters",
            ValidationErrorKind.StringTooLong => $"at most {schema?.MaxLength} characters",
            ValidationErrorKind.TooFewItems => $"at least {schema?.MinItems} items",
            ValidationErrorKind.TooManyItems => $"at most {schema?.MaxItems} items",
            ValidationErrorKind.PatternMismatch => $"has to match {schema?.Pattern}",
            ValidationErrorKind.NotInEnumeration => $"one of {string.Join(", ", schema?.Enumeration ?? [])}",
            // The other kinds read well enough as words : "IntegerExpected" as "integer expected".
            _ => Words(error.Kind.ToString()),
        };
    }

    private static string Invariant(FormattableString text) => FormattableString.Invariant(text);

    private static string Words(string name) => Regex.Replace(name, "(?<!^)([A-Z])", " $1").ToLowerInvariant();
}
