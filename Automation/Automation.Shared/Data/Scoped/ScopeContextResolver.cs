using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Scoped;

/// <summary>
/// Context declared on the scopes : a flat set of values every workflow the scope contains starts
/// from, read through <c>$context.*</c> in the tasks settings.
/// </summary>
public static class ScopeContextResolver
{
    /// <summary>
    /// Resolved context of a scope : the context of each scope of [hierarchy] (ordered from the root
    /// down to the scope itself) merged on top of its parents one, the references it holds (e.g.
    /// <c>"$errorMail"</c>) being replaced by the value they point at in the parents context.
    /// <para>
    /// A value left null is not merged : declaring a key without a value doesn't erase the one
    /// inherited from a parent scope.
    /// </para>
    /// </summary>
    public static JObject Resolve(IEnumerable<Scope> hierarchy)
    {
        JObject resolved = new JObject();
        foreach (Scope scope in hierarchy)
        {
            if (string.IsNullOrWhiteSpace(scope.ContextJson))
                continue;

            if (JToken.Parse(scope.ContextJson) is not JObject context)
                continue;

            // References only reach the parents context : the values of the scope itself are
            // exactly what is being declared here.
            JToken values = ReferencesHandler.ReplaceReferences(context, resolved).ReplacedSetting;
            resolved.Merge(values, MergeSettings);
        }

        return resolved;
    }

    private static readonly JsonMergeSettings MergeSettings = new JsonMergeSettings
    {
        MergeArrayHandling = MergeArrayHandling.Replace,
        MergeNullValueHandling = MergeNullValueHandling.Ignore
    };
}
