using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Remote;

/// <summary>
/// How the data travels between the applications, the supervisor and the workers, whatever the
/// channel (HTTP or SignalR) : a type is written the same way everywhere.
/// </summary>
public static class FantocheJson
{
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    /// <summary>
    /// [value] as the other end of a channel reads it : what a message carries without crossing
    /// one, so it is never shared between its sender and its receiver.
    /// </summary>
    public static T Copy<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;

    /// <summary>
    /// Add to [options] what the model needs : the JSON values (<see cref="JToken"/>) the instances
    /// and the contexts are made of.
    /// </summary>
    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JTokenConverterFactory());
        options.AllowOutOfOrderMetadataProperties = true;
        return options;
    }

    private sealed class JTokenConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeof(JToken).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType = typeof(JTokenConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    /// <summary>
    /// A null token is written as a JSON null and read back as no token at all : an output that is
    /// null (deactivated) has to stay so, see <see cref="Data.Execution.TaskInstance.Output"/>.
    /// </summary>
    private sealed class JTokenConverter<T> : JsonConverter<T> where T : JToken
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            string raw = document.RootElement.GetRawText();
            // Kept as written : a text looking like a date stays a text.
            using Newtonsoft.Json.JsonTextReader tokenReader = new(new StringReader(raw)) { DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
            return (T)JToken.ReadFrom(tokenReader);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteRawValue(value.ToString(Newtonsoft.Json.Formatting.None));
    }
}
