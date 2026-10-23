using Fantoche.App.Common;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Joufflu.Data.Model;
using Joufflu.Feedback;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel : ScopedDetailsViewModel<Scope>
    {
        public Scope Scope => Element;

        /// <summary>
        /// The parameters of the scope (<see cref="Scope.ContextJson"/>) : the global context every
        /// task and workflow under it starts from, over the one of its parents (see
        /// <see cref="ScopeContextResolver"/>).
        /// </summary>
        public DataObject Parameters { get; }

        /// <summary>
        /// The references the parameters hold (e.g. "$errorMail", a value of a parent scope), kept as
        /// references rather than loaded as the text they are written as.
        /// </summary>
        public IReadOnlyList<DataManualValue> References { get; }

        public ScopeDetailsViewModel(
            ScopedNode node,
            WorkflowsViewModel parent,
            IScopedService scoped,
            IHistoryService history,
            IOverlayer overlays,
            IToastService toasts)
            : base(node, parent, scoped, history, overlays, toasts)
        {
            JToken? context = Json.Parse(Scope.ContextJson);
            References = [.. Json.ReferencesIn(context).Distinct().Select(reference => new DataManualValue(null, reference))];
            Parameters = context is JObject values && values.ToDataNode() is DataObject tree ? tree : new DataObject(null);
            if (context != null)
                Parameters.Load(context, References);

            Parameters.Changed += (_, _) =>
            {
                Scope.ContextJson = ContextJsonOf(Parameters);
                MarkChanged();
            };
        }

        /// <summary>Null once every parameter is removed : the scope then declares nothing.</summary>
        private static string? ContextJsonOf(DataObject parameters)
            => parameters.Properties.Count == 0 ? null : parameters.ToToken()!.ToString(Formatting.Indented);
    }
}
