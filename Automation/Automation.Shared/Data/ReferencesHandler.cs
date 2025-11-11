using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data;

public class ReferenceReplaceError
{
    public string Reference { get; }
    public string Message { get; }

    public ReferenceReplaceError(string reference, string message)
    {
        Reference = reference;
        Message = message;
    }

    public override string ToString()
    {
        return $"{Reference} : {Message}";
    }
}

public class ReferenceReplaceContext
{
    public List<ReferenceReplaceError> Errors { get; } = [];
    public string ReplacedSetting { get; set; } = "";
    public Dictionary<string, JTokenType> ReferencesTypes { get; } = [];
}

public class MultiReferenceReplaceContext
{
    public List<ReferenceReplaceContext> Contexts { get; set; } = [];
    public List<ReferenceReplaceError> InconsistentReferenceErrors { get; set; } = [];
}

public static class ReferencesHandler
{
    private const string ReferenceIdentifier = "$";

    public static MultiReferenceReplaceContext ReplaceReferences(string settingJson, IEnumerable<string?> contextsJson)
    {
        List<ReferenceReplaceContext> results = [];
        foreach (var contextJson in contextsJson)
            results.Add(ReplaceReferences(settingJson, contextJson));

        var allKeys = results
            .SelectMany(d => d.ReferencesTypes.Keys)
            .Distinct()
            .ToList();

        var inconsistentKeys = allKeys
            .Where(key =>
            {
                var types = results
                    .Select(d => d.ReferencesTypes.TryGetValue(key, out var value) ? (JTokenType?)value : null)
                    .Distinct();
                // Inconsistent if: has null (missing) OR multiple different types
                return types.Contains(null) || types.Count(t => t.HasValue) > 1;
            })
            .Select(x => new ReferenceReplaceError(x, "Inconsistent types across contexts.")).ToList();

        return new MultiReferenceReplaceContext { Contexts = results, InconsistentReferenceErrors = inconsistentKeys };
    }

    /// <summary>
    /// Replace references by their actual context value (if the reference path exist in the context).
    /// </summary>
    /// <param name="settingJson">Setting containing references</param>
    /// <param name="contextJson">Context the references points to</param>
    /// <returns></returns>
    public static ReferenceReplaceContext ReplaceReferences(string settingJson, string? contextJson)
    {
        if (string.IsNullOrEmpty(settingJson) || string.IsNullOrEmpty(contextJson))
            return new ReferenceReplaceContext { ReplacedSetting = settingJson };

        var setting = JToken.Parse(settingJson);
        var context = JToken.Parse(contextJson);

        return ReplaceReferences(setting, context);
    }

    private static ReferenceReplaceContext ReplaceReferences(JToken token, JToken context)
    {
        var result = new ReferenceReplaceContext();
        ReplaceReferences(token, context, result);
        result.ReplacedSetting = token.ToString();
        return result;
    }

    /// <summary>
    /// Replace references by their actual context value (if the reference path exist in the context).
    /// </summary>
    /// <param name="token">Setting containing references</param>
    /// <param name="context">Context the references points to</param>
    /// <returns></returns>
    private static void ReplaceReferences(JToken token, JToken context, ReferenceReplaceContext result)
    {
        var reference = GetReferencePath(token);
        if (!string.IsNullOrEmpty(reference))
        {
            var contextValue = context.SelectToken(reference);
            if (contextValue != null)
            {
                token.Replace(contextValue);
                result.ReferencesTypes.Add(reference, contextValue.Type);
            }
            else
            {
                result.Errors.Add(new ReferenceReplaceError(reference, $"[{reference}] not found in context."));
            }
        }

        foreach (var child in token.Children()) ReplaceReferences(child, context, result);
    }

    private static string? GetReferencePath(JToken token)
    {
        if (token.Type != JTokenType.String)
            return null;
        var value = token.Value<string>();
        if (value == null)
            return null;
        if (value.StartsWith(ReferenceIdentifier) == false)
            return null;

        return value.Substring(ReferenceIdentifier.Length);
    }
}