using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// What the graph would run into : the nodes whose mapping cannot resolve what they read, and
    /// the branches they cannot resolve it on. Walked from the graph rather than from a run, so it
    /// shows while the workflow is being drawn.
    /// </summary>
    public class GraphPreviewer
    {
        /// <summary>
        /// The last walk of the graph, null when it could not be walked at all. Held rather than
        /// rebuilt by whoever needs it : two previews of the same graph are two walks of it, and
        /// they can disagree — the global context lands asynchronously, so one built before it
        /// arrives resolves "$global" against nothing.
        /// </summary>
        public GraphExecutionPreview? Preview { get; private set; }

        /// <summary>
        /// The context of the scopes holding the workflow, which a mapping reads as "$global".
        /// </summary>
        private JToken? _globalContext;

        /// <summary>
        /// The tasks the nodes of the graph point at : walking the graph needs them, a node handing
        /// over nothing of its own being only known through them.
        /// </summary>
        private Dictionary<Guid, BaseAutomationTask> _tasks = [];

        public void Load(JToken? globalContext, Dictionary<Guid, BaseAutomationTask> tasks)
        {
            _globalContext = globalContext;
            _tasks = tasks;
        }

        /// <summary>
        /// Walk [graph] again and hand each node and each connection what the walk holds against it.
        /// </summary>
        public void Refresh(
            TasksGraph graph,
            IReadOnlyCollection<NodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections)
        {
            Preview = Walk(graph);

            foreach (NodeViewModel node in nodes)
                node.Errors = Distinct(Find(Preview?.NodesErrors, node.Model.Id).Select(x => x.Message));

            foreach (ConnectionViewModel connection in connections)
            {
                // Named : an edge stands for what the node it leads to cannot handle rather than for
                // something of its own, so the reader needs to know which node holds it.
                connection.Errors = Distinct(Find(Preview?.EdgesErrors, connection.Model.Edge)
                    .Select(x => $"{NameOf(nodes, x.NodeId)} : {x.Message}"));
            }
        }

        private GraphExecutionPreview? Walk(TasksGraph graph)
        {
            try
            {
                // Re-wired first : a node added since the last load holds connectors nothing linked
                // to it yet, and walking the graph reads the nodes a connection leads to rather than
                // the ids it holds.
                graph.Refresh(_tasks, force: true);

                GraphContextResolution resolution = new() { GlobalContext = _globalContext };
                GraphExecutionPreview preview = new();
                preview.BuildSamples(graph, resolution);
                return preview;
            }
            catch
            {
                // A graph that can't be walked at all says nothing about its nodes : showing no
                // error is better than showing one on every one of them.
                return null;
            }
        }

        private static List<GraphPreviewError> Find<TKey>(
            Dictionary<TKey, List<GraphPreviewError>>? errors,
            TKey key)
            where TKey : notnull
            => errors != null && errors.TryGetValue(key, out List<GraphPreviewError>? found) ? found : [];

        /// <summary>
        /// The duplicates two branches carrying the same thing produce left out.
        /// </summary>
        private static IReadOnlyList<string> Distinct(IEnumerable<string> messages) => [.. messages.Distinct()];

        private static string NameOf(IReadOnlyCollection<NodeViewModel> nodes, Guid nodeId)
            => nodes.FirstOrDefault(x => x.Model.Id == nodeId)?.Name ?? "?";
    }
}
