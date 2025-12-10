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
    public JToken ReplacedSetting { get; set; }
    public List<ReferenceReplaceError> Errors { get; } = [];
    public Dictionary<string, JTokenType> ReferencesTypes { get; } = [];

    public ReferenceReplaceContext(JToken replacedSetting)
    {
        ReplacedSetting = replacedSetting;
    }
    
    public void SetReferenceType(string reference, JTokenType type)
    {
        if (!ReferencesTypes.TryAdd(reference, type))
            ReferencesTypes[reference] = type;
    }
}

public class MultiReferenceReplaceContext
{
    public List<ReferenceReplaceContext> Contexts { get; set; } = [];
    public List<ReferenceReplaceError> InconsistentReferenceErrors { get; set; } = [];
}

// TODO : separate Global, Workflow and previous and allow recursive references
public static class ReferencesHandler
{
    private const string ReferenceIdentifier = "$";

    static ReferencesHandler()
    {
    }

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
                    .Distinct()
                    .ToList();
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
            return new ReferenceReplaceContext(JToken.Parse(settingJson));

        var setting = JToken.Parse(settingJson);
        var context = JToken.Parse(contextJson);

        return ReplaceReferences(setting, context);
    }

    public static ReferenceReplaceContext ReplaceReferences(JToken setting, JToken? context)
    {
        if (context == null)
            return new ReferenceReplaceContext(setting);

        var result = new ReferenceReplaceContext(setting);
        ReplaceReferences(setting, context, result);
        result.ReplacedSetting = setting;
        return result;
    }

    /// <summary>
    /// Replace references by their actual context value (if the reference path exist in the context).
    /// </summary>
    /// <param name="token">Setting containing references</param>
    /// <param name="context">Context the references points to</param>
    /// <param name="result"></param>
    /// <returns></returns>
    private static void ReplaceReferences(JToken token, JToken context, ReferenceReplaceContext result)
    {
        var reference = GetReferencePath(token);
        if (!string.IsNullOrEmpty(reference))
        {
            var contextToken = context.SelectToken(reference);
            if (contextToken != null)
            {
                token.Replace(contextToken);
                result.SetReferenceType(reference, contextToken.Type);
                            
                // Recursive reference
                if (IsReference(contextToken))
                    ReplaceReferences(contextToken, context, result);
            }
            else
            {
                result.Errors.Add(new ReferenceReplaceError(reference, $"[{reference}] not found in context."));
            }
        }

        foreach (var child in token.Children()) ReplaceReferences(child, context, result);
    }

    /// <summary>
    /// Check if a token is a reference to the context.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static bool IsReference(JToken token)
    {
        if (token.Type != JTokenType.String)
            return false;
        var value = token.Value<string>();
        if (value == null)
            return false;
        if (value.StartsWith(ReferenceIdentifier) == false)
            return false;
        return true;
    }
    
    /// <summary>
    /// Get the reference path if the token contain a reference.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
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