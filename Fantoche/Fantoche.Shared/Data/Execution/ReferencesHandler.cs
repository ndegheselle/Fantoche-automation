using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Shared.Data;

public class ReferenceReplaceResult
{
    public JToken Replaced { get; set; }
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;

    public ReferenceReplaceResult(JToken mapping, List<string> errors)
    {
        Replaced = mapping;
        Errors = errors;
    }
}

public static class ReferencesHandler
{
    private const string ReferenceIdentifier = "$";

    static ReferencesHandler()
    {
    }

    /// <param name="schema">
    /// Schema of what the template produces : the properties it marks as evaluated per row (see
    /// <see cref="RowScope"/>) are the only ones where a row reference is allowed.
    /// </param>
    public static ReferenceReplaceResult ReplaceReferences(JToken template, JToken context, JsonSchema? schema = null)
    {
        IReadOnlyList<string> rowScoped = RowScopedProperties(schema);
        var resultToken = ReplaceReferences(template, context, rowScoped, out var errors);
        var result = new ReferenceReplaceResult(resultToken, errors);
        return result;
    }

    /// <summary>
    /// Replace references by their actual context value (if the reference path exist in the context).
    /// </summary>
    /// <param name="template">Setting containing references</param>
    /// <param name="context">Context the references points to</param>
    /// <param name="result"></param>
    /// <returns></returns>
    private static JToken ReplaceReferences(JToken template, JToken context, IReadOnlyList<string> rowScoped, out List<string> errors)
    {
        errors = [];
        // Only the task iterating the table knows the row, it resolves these references itself.
        if (template is JValue { Value: string value } && RowScope.IsReference(value))
        {
            if (!rowScoped.Any(property => IsUnder(template.Path, property)))
                errors.Add($"[{value}] is only available in a field evaluated per row.");
            return template;
        }

        var reference = GetReferencePath(template);
        // Is a reference
        if (!string.IsNullOrEmpty(reference))
        {
            var contextToken = context.SelectToken(reference);
            if (contextToken == null)
            {
                errors.Add($"[{reference}] not found in context.");
                return template;
            }

            template.Replace(contextToken);

            // Recursive reference
            if (IsReference(contextToken))
                ReplaceReferences(contextToken, context, rowScoped, out errors);
        }
        else
        {
            foreach (var child in template.Children())
            {
                ReplaceReferences(child, context, rowScoped, out var childErrors);
                errors.AddRange(childErrors);
            }

        }
        return template;
    }

    private static IReadOnlyList<string> RowScopedProperties(JsonSchema? schema)
    {
        if (schema == null)
            return [];

        return [.. schema.ActualSchema.ActualProperties
            .Where(property => property.Value.ExtensionData?.TryGetValue(RowScope.SchemaKey, out object? scope) == true
                && Equals(scope, RowScope.Name))
            .Select(property => property.Key)];
    }

    private static bool IsUnder(string path, string property)
        => path == property || path.StartsWith($"{property}.") || path.StartsWith($"{property}[");

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