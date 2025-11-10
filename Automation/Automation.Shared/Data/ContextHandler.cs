using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data
{
    public class ContextReplaceError
    {
        public string Reference { get; }
        public string Message { get; }
        public ContextReplaceError(string reference, string message)
        {
            Reference = reference;
            Message = message;
        }
    }
    
    public static class ContextHandler
    {
        private const string ReferenceIdentifier = "$";
        
        /// <summary>
        /// Replace references by their actual context value (if the reference path exist in the context).
        /// </summary>
        /// <param name="settingJson">Setting containing references</param>
        /// <param name="contextJson">Context the references points to</param>
        /// <returns></returns>
        public static string ReplaceReferences(string settingJson, string? contextJson, out List<ContextReplaceError> errors)
        {
            errors = new List<ContextReplaceError>();
            if (string.IsNullOrEmpty(settingJson) || string.IsNullOrEmpty(contextJson))
                return settingJson;

            JToken setting = JToken.Parse(settingJson);
            ReplaceReferences(setting, JToken.Parse(contextJson), out errors);

            return setting.ToString();
        }
        
        /// <summary>
        /// Replace references by their actual context value (if the reference path exist in the context).
        /// </summary>
        /// <param name="token">Setting containing references</param>
        /// <param name="context">Context the references points to</param>
        /// <returns></returns>
        private static void ReplaceReferences(JToken token, JToken context, out List<ContextReplaceError> errors)
        {
            errors = new List<ContextReplaceError>();
            string? reference = GetReferencePath(token);
            if (string.IsNullOrEmpty(reference) == false)
            {
                JToken? contextValue = context.SelectToken(reference);
                if (contextValue != null)
                    token.Replace(contextValue);
                else
                    errors.Add(new ContextReplaceError(reference, $"[{reference}] not found in context."));
            }

            foreach (JToken child in token.Children())
            {
                ReplaceReferences(child, context, out errors);
            }
        }
        
        private static string? GetReferencePath(JToken token)
        {
            if (token.Type == JTokenType.String)
                return null;
            string? value = token.Value<string>();
            if (value == null)
                return null;
            if (value.StartsWith(ReferenceIdentifier) == false)
                return null;
            
            return value.Substring(ReferenceIdentifier.Length);
        }
    }
}