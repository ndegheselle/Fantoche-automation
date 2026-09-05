using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Graph
{
    /// <summary>
    /// What a node reads : "$previous", "$shared" and "$global" as they stand where it runs. The
    /// executor fills them with what the branch actually produced and an editor with samples of
    /// what it would produce (see <see cref="GraphSampling"/>), so both hand the very same thing to
    /// the resolution of a mapping.
    /// </summary>
    /// <param name="Branch">
    /// Node the values come from, null when there is nothing to tell apart : nothing before the
    /// node, or every branch reaching it merged into one context.
    /// </param>
    /// <param name="Values">The context itself, as the references point at it.</param>
    public record GraphContext(string? Branch, JObject Values)
    {
        public const string PreviousIdentifier = "previous";
        public const string SharedIdentifier = "shared";
        public const string GlobalIdentifier = "global";

        /// <summary>
        /// The context of a node reading a single branch, [previous] being what it produced.
        /// </summary>
        public static GraphContext From(string? branch, JToken? previous, JToken? shared, JToken? global)
            => new(branch, new JObject
            {
                [PreviousIdentifier] = previous,
                [SharedIdentifier] = shared,
                [GlobalIdentifier] = global,
            });

        /// <summary>
        /// The context of a node merging every branch reaching it, what each produced being held
        /// under the name of the node it comes from.
        /// </summary>
        public static GraphContext From(Dictionary<string, JToken?> previous, JToken? shared, JToken? global)
        {
            JObject values = new()
            {
                [PreviousIdentifier] = new JObject(),
                [SharedIdentifier] = shared,
                [GlobalIdentifier] = global,
            };

            JObject branches = (JObject)values[PreviousIdentifier]!;
            foreach ((string branch, JToken? produced) in previous)
                branches[branch] = produced;

            return new GraphContext(null, values);
        }

        /// <summary>
        /// [template] with its references replaced by what they point at in this context : the
        /// parameters a task runs with, or the values a control hands over. Null when there is no
        /// template, the node then mapping nothing.
        /// </summary>
        public JToken? Resolve(JToken? template) => template == null ? null : Report(template).ReplacedSetting;

        /// <summary>
        /// The resolution of [template] along with what couldn't be resolved : what an editor tells
        /// about a mapping before it is ever run.
        /// </summary>
        public ReferenceReplaceContext Report(JToken template)
            // Resolved on a copy : the replacement writes the values into the token it walks.
            => ReferencesHandler.ReplaceReferences(template.DeepClone(), Values);

        /// <summary>
        /// Merge two contexts coming from two branches, the values of [other] winning. Anything that
        /// isn't an object can't be merged and is taken as-is.
        /// </summary>
        public static JToken? Merge(JToken? context, JToken? other)
        {
            if (other == null)
                return context;
            if (context is not JObject source || other is not JObject values)
                return other;

            JObject merged = (JObject)source.DeepClone();
            merged.Merge(values, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Ignore,
            });
            return merged;
        }

        /// <summary>
        /// The values [defaults] stands for, overwritten by what [values] actually holds : a value
        /// given by the caller wins over the default, a null included.
        /// </summary>
        public static JToken? ApplyDefaults(JToken? defaults, JToken? values)
        {
            // Only two objects can be merged key by key : anything else is given whole, and what
            // the caller gives wins. The defaults only stand in for what is missing.
            if (defaults is not JObject fallbacks)
                return values ?? defaults;
            if (values is not JObject given)
                return values ?? defaults;

            JObject merged = (JObject)fallbacks.DeepClone();
            merged.Merge(given, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Merge,
            });
            return merged;
        }
    }
}
