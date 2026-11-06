using System.Collections.ObjectModel;
using System.Windows;
using Fantoche.App.Common;
using Fantoche.App.Features.Workflows.Editor.History;
using Fantoche.Plugins.Shared.Ui;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Data.Model;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// The settings of a node whose task has an interface handling the editor (see
    /// <see cref="ITaskInterface"/>) : the interface edits the mapping, checked by
    /// <see cref="MappingCheck"/> as the generated settings are. Handed to the editor as an
    /// <see cref="IReversibleAction"/>, like them.
    /// </summary>
    public partial class TaskInterfaceSetupViewModel : ObservableObject, IOverlayContent<IReversibleAction>
    {
        private readonly BaseGraphTask _node;
        private readonly ITaskInterface _interface;
        private readonly IReadOnlyList<NodePreviewContext> _contexts;
        private readonly IOverlayer _overlayer;

        public OverlayOptions Options { get; }

        public IReversibleAction? Result { get; private set; }

        public FrameworkElement View { get; }

        /// <summary>What is wrong with what the interface handed, refusing it.</summary>
        public ObservableCollection<MappingError> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private bool _isValidating;

        private TaskInterfaceSetupViewModel(BaseGraphTask node, ITaskInterface taskInterface, FrameworkElement view, IReadOnlyList<NodePreviewContext> contexts, IOverlayer overlayer)
        {
            _node = node;
            _interface = taskInterface;
            _contexts = contexts;
            _overlayer = overlayer;
            View = view;
            Options = new OverlayOptions() { Title = node.Name };
            Errors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// Show the settings of [node] with the interface of its task, or with the generated settings
        /// when it has none, doesn't handle the editor or can't be loaded. The edition to apply to
        /// the graph, null when cancelled.
        /// </summary>
        public static async Task<IReversibleAction?> ShowAsync(BaseGraphTask node, GraphExecutionPreview? preview)
        {
            if (node.AutomationTask?.Interface is not PackageClassTarget target)
                return await TaskSettingsViewModel.ShowAsync(node, preview);

            IReadOnlyList<NodePreviewContext> contexts = preview?.NodesContexts.GetValueOrDefault(node.Id) ?? [];
            TaskInterfaceSetupViewModel settings;
            try
            {
                ITaskInterface taskInterface = await SpineViewModel.Instance.Interfaces.CreateAsync(target);
                if (!taskInterface.Handles(EnumInterfaceMode.Setup))
                    return await TaskSettingsViewModel.ShowAsync(node, preview);

                TaskInterfaceContext context = new()
                {
                    Mode = EnumInterfaceMode.Setup,
                    Parameters = Json.Parse(node.InputTemplateJson),
                    References = ReferencesOf(node, contexts),
                };
                FrameworkElement view = taskInterface.Show(context);
                settings = new TaskInterfaceSetupViewModel(node, taskInterface, view, contexts, SpineViewModel.Instance.Overlayer);
            }
            catch (Exception exception)
            {
                // The node can still be set up : the generated settings edit the same mapping.
                SpineViewModel.Instance.Toasting.Warning(exception.Message, "The interface of the task can't be shown");
                return await TaskSettingsViewModel.ShowAsync(node, preview);
            }

            return await SpineViewModel.Instance.Overlayer.ShowAsync(settings);
        }

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

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private async Task Validate()
        {
            Errors.Clear();
            IsValidating = true;
            try
            {
                JToken mapping = await _interface.ReadAsync() ?? new JObject();
                List<MappingError> errors = MappingCheck.Check(mapping, _contexts, _node.InputSchema, allowMissingRequired: false);
                foreach (MappingError error in errors)
                    Errors.Add(error);
                if (HasErrors)
                    return;

                Result = EditionOf(mapping.ToString(Formatting.Indented));
                _overlayer.Validate(this);
            }
            catch (Exception exception)
            {
                // A TaskInterfaceException tells what is left to fix ; anything else is a bug of the
                // interface, shown the same way rather than losing what the user set.
                Errors.Add(new MappingError(exception.Message));
            }
            finally
            {
                IsValidating = false;
            }
        }

        private bool CanValidate() => !IsValidating;

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        /// <summary>The edition holds both mappings, so the editor can undo it.</summary>
        private IReversibleAction EditionOf(string mapping)
        {
            string? previousMapping = _node.InputTemplateJson;
            return new ReversibleAction(
                $"Edit '{_node.Name}'",
                () => _node.InputTemplateJson = mapping,
                () => _node.InputTemplateJson = previousMapping);
        }
    }
}
