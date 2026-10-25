using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Graph;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// What a preview takes a task to hand over without running it : the sample of its output schema,
/// with the parts of its input it declares handing over (see <see cref="OutputPreview"/>).
/// </summary>
public static class OutputSamples
{
    /// <summary>
    /// The output of [node] resolved with [input], as the nodes after it read it in a preview.
    /// </summary>
    public static JToken? Of(BaseGraphTask node, JToken? input)
    {
        JToken? sample = node.OutputSchema?.ToSampleJson();
        if (node.InputSchema == null || input is not JObject values)
            return sample;

        IReadOnlyDictionary<string, JsonSchemaProperty> properties = node.InputSchema.ActualSchema.ActualProperties;

        // The values first : a mapping is written on the table handed over, not replaced by it.
        HashSet<string> handedOver = [];
        foreach (var (name, property) in properties)
        {
            if (PlaceOf(property, OutputPreview.SchemaKey) is string place && values[name] is JToken value)
                sample = HandOver(sample, place, value, handedOver);
        }

        foreach (var (name, property) in properties)
        {
            if (PlaceOf(property, OutputPreview.MappingSchemaKey) is string place && values[name] is JArray columns)
                sample = HandOver(sample, place, new JObject { [nameof(TableRef.Mapping)] = MappingOf(columns) }, handedOver);
        }

        return sample;
    }

    private static string? PlaceOf(JsonSchemaProperty property, string key)
        => property.ExtensionData?.TryGetValue(key, out object? place) == true ? place as string : null;

    /// <summary>
    /// [value] put at [place] of [output]. The first value handed over to a place replaces the
    /// sample there, which only holds placeholders, the next ones are merged with it.
    /// </summary>
    private static JToken? HandOver(JToken? output, string place, JToken value, HashSet<string> handedOver)
    {
        bool isFirst = handedOver.Add(place);
        if (place == OutputPreview.Whole)
            return isFirst ? value.DeepClone() : GraphContextResolution.MergeContexts(output, value);

        JObject values = output as JObject ?? [];
        values[place] = isFirst ? value.DeepClone() : GraphContextResolution.MergeContexts(values[place], value);
        return values;
    }

    /// <summary>[columns], a list of <see cref="ColumnMapping"/>, as the dictionary a table holds.</summary>
    private static JObject MappingOf(JArray columns)
    {
        JObject mapping = [];
        foreach (JToken column in columns)
        {
            if (column[nameof(ColumnMapping.Name)]?.Value<string>() is not { Length: > 0 } name)
                continue;
            mapping[name] = column[nameof(ColumnMapping.Column)]?.DeepClone() ?? "";
        }
        return mapping;
    }
}
