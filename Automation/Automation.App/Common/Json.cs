using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automation.App.Common
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
        /// An empty text box means nothing is mapped, which is null rather than "".
        /// </summary>
        public static string? NullIfEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? null : json;
    }
}
