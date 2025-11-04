using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data
{
    public static class ContextHandler
    {
        private const string ReferenceIdentifier = "$";
        /// <summary>
        /// Replace references by their actual context value (if the reference path exist in the context).
        /// </summary>
        /// <param name="settingJson">Setting containing references</param>
        /// <param name="contextJson">Context the references points to</param>
        /// <returns></returns>
        public static string ReplaceReferences(string settingJson, string? contextJson)
        {
            if (string.IsNullOrEmpty(settingJson) || string.IsNullOrEmpty(contextJson))
                return settingJson;

            JToken setting = JToken.Parse(settingJson);
            ReplaceReferences(setting, JToken.Parse(contextJson));

            return setting.ToString();
        }
        
        /// <summary>
        /// Replace references by their actual context value (if the reference path exist in the context).
        /// </summary>
        /// <param name="token">Setting containing references</param>
        /// <param name="context">Context the references points to</param>
        /// <returns></returns>
        private static void ReplaceReferences(JToken token, JToken context)
        {
            if (token.Type == JTokenType.String)
            {
                string? value = token.Value<string>();
                if (value != null && value.StartsWith(ReferenceIdentifier))
                {
                    string path = value.Substring(ReferenceIdentifier.Length);
                    JToken? contextValue = context.SelectToken(path);
                    if (contextValue != null)
                    {
                        token.Replace(contextValue);
                    }
                }
            }

            foreach (JToken child in token.Children())
            {
                ReplaceReferences(child, context);
            }
        }
    }
}