using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data
{
    public class ContextHandler
    {
        public const string REFERENCE_IDENTIFIER = "$";

        public static string ReplaceContext(string settingJson, string? contextJson)
        {
            if (string.IsNullOrEmpty(settingJson) || string.IsNullOrEmpty(contextJson))
                return settingJson;

            JToken setting = JToken.Parse(settingJson);
            JToken context = JToken.Parse(contextJson);

            Crawl(setting, context);

            return setting.ToString();
        }

        private static void Crawl(JToken token, JToken context)
        {
            if (token.Type == JTokenType.String)
            {
                string? value = token.Value<string>();
                if (value != null && value.StartsWith(REFERENCE_IDENTIFIER))
                {
                    string path = value.Substring(REFERENCE_IDENTIFIER.Length);
                    JToken? contextValue = context.SelectToken(path);
                    if (contextValue != null)
                    {
                        token.Replace(contextValue);
                    }
                }
            }

            foreach (var child in token.Children())
            {
                Crawl(child, context);
            }
        }
    }
}