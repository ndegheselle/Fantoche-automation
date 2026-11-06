using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// One thing wrong with a mapping. [Branch] is set when the node is reached more than one way : a
/// mapping can hold up on one branch and break on another.
/// </summary>
public record MappingError(string Message, string? Branch = null)
{
    public bool HasBranch => Branch != null;
}

/// <summary>
/// Checks a node's mapping the way a run resolves it, so an editor knows what is wrong without
/// running anything.
/// </summary>
public static class MappingCheck
{
    /// <summary>
    /// Resolve [mapping] against every context the node can be reached with and check what comes
    /// out against [expected].
    /// </summary>
    /// <param name="allowMissingRequired">
    /// The values of the start are only defaults for what the caller leaves out, so they may skip
    /// a required property.
    /// </param>
    public static List<MappingError> Check(
        JToken mapping,
        IReadOnlyList<NodePreviewContext> contexts,
        JsonSchema? expected,
        bool allowMissingRequired)
    {
        List<MappingError> errors = [];
        List<string> referenced = ReferencedPaths(mapping);
        foreach (NodePreviewContext context in contexts)
        {
            string? branch = BranchLabel(context, contexts);

            // Resolving replaces the references in place, so each context gets its own copies.
            JToken template = mapping.DeepClone();
            JObject values = (JObject)context.Context.DeepClone();

            ReferenceReplaceResult result = ReferencesHandler.ReplaceReferences(template, values, expected);
            errors.AddRange(result.Errors.Select(error => new MappingError(error, branch)));

            // Unresolved references would be validated as the plain strings they are written as.
            if (expected == null || result.HasErrors)
                continue;

            errors.AddRange(SchemaErrors(expected, result.Replaced, referenced, allowMissingRequired, branch));
        }
        return errors;
    }

    /// <summary>
    /// The errors about a value rather than its type. A referenced value is resolved to a sample in
    /// the editor, which knows nothing of what the run will read : only its type is worth checking.
    /// </summary>
    private static readonly HashSet<ValidationErrorKind> ValueKinds =
    [
        ValidationErrorKind.NumberTooSmall,
        ValidationErrorKind.NumberTooBig,
        ValidationErrorKind.NumberNotMultipleOf,
        ValidationErrorKind.StringTooShort,
        ValidationErrorKind.StringTooLong,
        ValidationErrorKind.PatternMismatch,
        ValidationErrorKind.NotInEnumeration,
        ValidationErrorKind.TooFewItems,
        ValidationErrorKind.TooManyItems,
        ValidationErrorKind.ItemsNotUnique,
    ];

    /// <summary>The paths of [mapping] holding a reference, as <see cref="JToken.Path"/> writes them.</summary>
    private static List<string> ReferencedPaths(JToken mapping)
        => [.. (mapping as JContainer)?.DescendantsAndSelf()
            .Where(ReferencesHandler.IsReference)
            .Select(token => token.Path) ?? []];

    /// <summary>
    /// The branches feeding the node through [context], null when there is only one way in and so
    /// nothing to tell apart.
    /// </summary>
    public static string? BranchLabel(NodePreviewContext context, IReadOnlyList<NodePreviewContext> contexts)
        => contexts.Count > 1 && context.Branches.Count > 0 ? string.Join(", ", context.Branches) : null;

    private static IEnumerable<MappingError> SchemaErrors(
        JsonSchema expected,
        JToken resolved,
        List<string> referenced,
        bool allowMissingRequired,
        string? branch)
    {
        string json = resolved.ToString(Formatting.None);
        return expected.Validate(json)
            .Where(error => !allowMissingRequired || error.Kind != ValidationErrorKind.PropertyRequired)
            .Where(error => !IsAboutReferencedValue(error, referenced))
            .Select(error => new MappingError(Schemas.Describe(error), branch));
    }

    private static bool IsAboutReferencedValue(ValidationError error, List<string> referenced)
    {
        if (!ValueKinds.Contains(error.Kind))
            return false;

        string path = Schemas.PathOf(error);
        return referenced.Any(reference => ReferencesHandler.IsUnder(path, reference));
    }
}
