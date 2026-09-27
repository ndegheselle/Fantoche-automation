using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Common
{
    public static class Json
    {
        /// <summary>
        /// [json] indented, or as it was written when it can't be read as JSON : whoever typed it is
        /// where that is reported, not wherever it is displayed.
        /// </summary>
        public static string Format(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "";

            try
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
            catch (Exception)
            {
                return json;
            }
        }

        /// <summary>
        /// [token] indented, or the text itself when it holds one : a failure is stored as its stack
        /// trace, which is not worth quoting and escaping.
        /// </summary>
        public static string Format(JToken? token)
        {
            if (token == null)
                return "";

            return token.Type == JTokenType.String
                ? token.ToString()
                : token.ToString(Formatting.Indented);
        }

        /// <summary>
        /// [json] as a token, null when there is none or it isn't JSON. Dates are kept as the text
        /// they are written as, a Joufflu.Data tree converting them where its schema says so.
        /// </summary>
        public static JToken? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var reader = new JsonTextReader(new System.IO.StringReader(json)) { DateParseHandling = DateParseHandling.None };
                return JToken.ReadFrom(reader);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// The context references [token] holds (e.g. "$previous.Value"), in the order they are met.
        /// </summary>
        public static IEnumerable<string> ReferencesIn(JToken? token)
            => (token as JContainer)?.DescendantsAndSelf()
                .Where(value => value.Type == JTokenType.String)
                .Select(value => (string)value!)
                .Where(value => value.StartsWith('$')) ?? [];
    }
}
