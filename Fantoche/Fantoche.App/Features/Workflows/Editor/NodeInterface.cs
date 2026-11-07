using System.Windows;
using Fantoche.App.Common;
using Fantoche.App.Features.UserInterfaces;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Joufflu.Data.Model;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// The interface the task of a node picks (see <see cref="ITaskInterface"/>), shown by
    /// <see cref="TaskSettingsViewModel"/> where the generated form would be, in
    /// <see cref="EnumInterfaceMode.Setup"/> : it edits the mapping of the node.
    /// </summary>
    public sealed class NodeInterface
    {
        private readonly ITaskInterface _interface;

        public FrameworkElement View { get; }

        private NodeInterface(ITaskInterface taskInterface, FrameworkElement view)
        {
            _interface = taskInterface;
            View = view;
        }

        /// <summary>
        /// The interface of the task of [node] built on its mapping, null for the generated form :
        /// the task picks none, the interface doesn't handle the setup, or it can't be shown.
        /// </summary>
        public static async Task<NodeInterface?> CreateAsync(BaseGraphTask node, IReadOnlyList<NodePreviewContext> contexts)
        {
            if (node.AutomationTask?.Interface is not PackageClassTarget target)
                return null;

            try
            {
                ITaskInterface taskInterface = await SpineViewModel.Instance.Interfaces.CreateAsync(target);
                if (!taskInterface.Handles(EnumInterfaceMode.Setup))
                    return null;

                TaskInterfaceContext context = new()
                {
                    Mode = EnumInterfaceMode.Setup,
                    Parameters = Json.Parse(node.InputTemplateJson),
                    References = ReferencesOf(node, contexts),
                };
                FrameworkElement view = TaskInterfaces.ViewOf(taskInterface, context);
                return new NodeInterface(taskInterface, view);
            }
            catch (Exception exception)
            {
                // The node can still be set up : the generated form edits the same mapping.
                SpineViewModel.Instance.Toasting.Warning(exception.Message, "The interface of the task can't be shown");
                return null;
            }
        }

        /// <summary>The mapping the user confirmed, an empty object when the interface hands none.</summary>
        /// <exception cref="TaskInterfaceException">The user has something left to fix.</exception>
        public async Task<JToken> ReadAsync() => await _interface.ReadAsync() ?? new JObject();

        /// <summary>
        /// What the node reads, typed by what the preview holds there, and the columns read per row
        /// (untyped, see <see cref="RowTables"/>). A value holding null is left out : untyped, it
        /// would be offered to every field.
        /// </summary>
        private static List<TaskReference> ReferencesOf(BaseGraphTask node, IReadOnlyList<NodePreviewContext> contexts)
        {
            IEnumerable<TaskReference> read = contexts
                .SelectMany(context => context.Context.Properties())
                .SelectMany(property => TaskSettingsViewModel.ReferencesTo(property.Value, $"${property.Name}"))
                .Where(entry => entry.Type != null)
                .Select(entry => new TaskReference((string)entry.Value!, JsonTypeOf(entry.Type!.Value)));

            JToken? template = Json.Parse(node.InputTemplateJson);
            IEnumerable<TaskReference> rows = template == null
                ? []
                : RowTables.Offered(template, contexts.Select(x => x.Context), node.InputSchema).Select(reference => new TaskReference(reference, null));

            return [.. read.Concat(rows).Distinct()];
        }

        private static string JsonTypeOf(EnumDataType type) => type switch
        {
            EnumDataType.Object => "object",
            EnumDataType.Array => "array",
            EnumDataType.Integer => "integer",
            EnumDataType.Number => "number",
            EnumDataType.Boolean => "boolean",
            _ => "string",
        };
    }
}
