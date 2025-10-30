using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Automation.Shared.Data
{
    internal class ContextHandler
    {
        public const string REFERENCE_IDENTIFIER = "$";

        public void ReplaceContext(string settingJson, string contextJson)
        {
            JToken setting = JToken.Parse(settingJson);
            JToken context = JToken.Parse(contextJson);

            Crawl(setting, context);
        }

        private void Crawl(JToken token, JToken context)
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