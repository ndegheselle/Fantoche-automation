using System.Collections.ObjectModel;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// One entry of the context a node reads : a value it can reference, or an object holding some.
    /// </summary>
    public class ContextEntry
    {
        public string Name { get; }

        /// <summary>
        /// What has to be written in the mapping to read this value, e.g. "$previous.Value".
        /// </summary>
        public string Reference { get; }

        /// <summary>
        /// An example of what the value holds, so the shape is readable without expanding it.
        /// </summary>
        public string Preview { get; }

        public ObservableCollection<ContextEntry> Children { get; } = [];

        public ContextEntry(string name, string reference, JToken? value)
        {
            Name = name;
            Reference = reference;
            Preview = Summarize(value);

            if (value is JObject values)
            {
                foreach (JProperty property in values.Properties())
                    Children.Add(new ContextEntry(property.Name, $"{reference}.{property.Name}", property.Value));
            }
        }

        /// <summary>
        /// What the value looks like in one line : an object is only worth its shape, the entries
        /// under it saying the rest.
        /// </summary>
        private static string Summarize(JToken? value) => value switch
        {
            null or { Type: JTokenType.Null } => "null",
            JObject => "{ }",
            JArray array => $"[ {array.Count} ]",
            _ => value.ToString(Formatting.None),
        };
    }

    /// <summary>
    /// Settings of a graph node : the mapping it runs with, edited as raw JSON between what it reads
    /// (the branches reaching it, the shared values and the context of its scopes) and what comes
    /// out of it once the references are resolved.
    /// <para>
    /// Nothing but the mapping is edited : the schemas of the graph are deduced from the mappings
    /// when the workflow is saved (see <see cref="AutomationWorkflow.DeriveSchemas"/>), and the one schema
    /// written by hand — the input of the workflow — belongs to its settings, the start only
    /// showing it.
    /// </para>
    /// <para>
    /// The settings are only written to the graph once validated, and as a
    /// <see cref="IReversibleAction"/> handed over to the editor : nothing is edited in place, so
    /// cancelling has nothing to restore and saving stays the editor's business.
    /// </para>
    /// </summary>
    public partial class TaskSettingsViewModel : ObservableObject
    {
        public BaseGraphTask Node { get; }

        /// <summary>
        /// Edition to apply to the graph, only set once the settings have been validated.
        /// </summary>
        public IReversibleAction? Edition { get; private set; }

        /// <summary>
        /// Mapping the node runs with, references to the context included (e.g. "$previous.Value").
        /// </summary>
        [ObservableProperty] private string? _inputMappingJson;

        /// <summary>
        /// What the mapping produces once its references are resolved : the parameters the task
        /// would run with, or the values the node hands over.
        /// </summary>
        [ObservableProperty] private string _resultJson = string.Empty;

        /// <summary>
        /// What the node reads, one root per branch reaching it : a reference is written from there.
        /// </summary>
        public ObservableCollection<ContextEntry> Context { get; } = [];

        /// <summary>
        /// What is wrong with the current mapping, blocking the validation while not empty.
        /// </summary>
        public ObservableCollection<string> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        public string Title { get; }

        /// <summary>
        /// What the node is about, displayed above the mapping : every node maps what it reads into
        /// what it hands over, only what is done with the result changes.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The start stands for the workflow itself : what it hands over is the input of the
        /// workflow and the default values are part of its settings, so there is nothing to edit
        /// here.
        /// </summary>
        public bool IsStart => _control?.IsStart() == true;

        public bool IsEditable => !IsStart;

        /// <summary>
        /// The node as a control task, null when it is a regular task or a nested workflow.
        /// </summary>
        private readonly GraphControl? _control;

        /// <summary>
        /// The graph as it would run : what every node reads and hands over, samples of the schemas
        /// and of the mappings the graph is made of.
        /// </summary>
        private readonly GraphSampling _sampling;

        private readonly IOverlayService _overlays;

        /// <summary>
        /// Open the settings of the workflow, where the start is edited. Null when the overlay was
        /// opened from somewhere that can't show them.
        /// </summary>
        private readonly Action? _openWorkflowSettings;

        public TaskSettingsViewModel(
            BaseGraphTask node,
            AutomationWorkflow workflow,
            IOverlayService overlays,
            JToken? globalContext = null,
            Action? openWorkflowSettings = null)
        {
            Node = node;
            _overlays = overlays;
            _control = node as GraphControl;
            _sampling = workflow.Sample(globalContext);
            _openWorkflowSettings = openWorkflowSettings;

            Title = $"{node.Name} - {Describe()}";
            Description = Explain();
            _inputMappingJson = node.InputMappingJson;

            LoadContext();

            Errors.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasErrors));
                ValidateCommand.NotifyCanExecuteChanged();
            };

            Refresh();
        }

        /// <summary>
        /// Show the settings of [node] and wait for the user to validate them, the edition to apply
        /// to the graph being returned (<see langword="null"/> when cancelled).
        /// </summary>
        public static async Task<IReversibleAction?> ShowAsync(
            BaseGraphTask node,
            AutomationWorkflow workflow,
            Action? openWorkflowSettings = null)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            // The context of the scopes holding the workflow is read once : the mapping can
            // reference it, so showing and checking it needs it.
            JToken? global = null;
            try
            {
                global = await SpineViewModel.Instance.Scoped.GetContextAsync(workflow.Id);
            }
            catch
            {
                // Without it a reference to the global context simply can't be resolved.
            }

            var viewModel = new TaskSettingsViewModel(node, workflow, overlays, global, openWorkflowSettings);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = viewModel.Title }) != true)
                return null;
            return viewModel.Edition;
        }

        /// <summary>
        /// What the node does with its mapping, which is the only thing telling the kinds apart.
        /// </summary>
        private string Describe()
        {
            if (_control == null)
                return "Task";
            if (_control.IsStart())
                return "Start";
            if (_control.IsEnd())
                return "End";
            if (_control.IsShare())
                return "Share";
            if (_control.IsJoin())
                return "Join";
            if (_control.IsMap())
                return "Map";
            return "Control";
        }

        private string Explain()
        {
            if (_control == null)
                return "The mapping is what the task runs with : it has to match what the task expects.";
            if (_control.IsStart())
                return "The start hands over what the workflow is started with. Its schema and its default values belong to the settings of the workflow.";
            if (_control.IsEnd())
                return "The mapping is what the workflow hands back to whoever started it.";
            if (_control.IsShare())
                return "The mapping is added to the shared values, readable as \"$shared\" by every node after this one. The branch itself goes through untouched.";
            if (_control.IsJoin())
                return "Every branch reaching the join is waited for, then merged into what the mapping describes.";
            return "The mapping reshapes what one branch produces into what the next ones read.";
        }

        /// <summary>
        /// Build what the node reads : one root per branch, holding "$previous", "$shared" and
        /// "$global" as they would be read from there.
        /// </summary>
        private void LoadContext()
        {
            Context.Clear();

            IReadOnlyList<GraphContext> contexts = _sampling.GetContexts(Node);
            foreach (GraphContext context in contexts)
            {
                // The branch is only worth naming when there is more than one way in.
                bool named = context.Branch != null && contexts.Count > 1;
                ContextEntry? branch = null;
                if (named)
                {
                    branch = new ContextEntry($"from {context.Branch}", string.Empty, null);
                    Context.Add(branch);
                }

                foreach (JProperty property in context.Values.Properties())
                {
                    var entry = new ContextEntry($"${property.Name}", $"${property.Name}", property.Value);
                    if (branch != null)
                        branch.Children.Add(entry);
                    else
                        Context.Add(entry);
                }
            }
        }

        /// <summary>
        /// Check the mapping and show what it produces : both are read from the graph as it would
        /// run, so nothing has to be executed to know.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            CheckJson("Input mapping", InputMappingJson);
            if (Errors.Count == 0)
                CheckInputMapping();

            ResultJson = Resolve();
        }

        /// <summary>
        /// Add an error when [json] is filled with something that isn't JSON. An empty value is
        /// valid, it simply means the node maps nothing.
        /// </summary>
        private void CheckJson(string label, string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                JToken.Parse(json);
            }
            catch (Exception exception)
            {
                Errors.Add($"{label} : {exception.Message}");
            }
        }

        /// <summary>
        /// Check the mapping as it would be resolved when the workflow runs : the references are
        /// replaced by samples of what the node reads, and what comes out has to match the schema
        /// the node is expected to hand over.
        /// </summary>
        private void CheckInputMapping()
        {
            if (IsStart)
                return;

            // Only a task expects a shape : everywhere else the schema is deduced from the mapping
            // itself when the workflow is saved, so there is nothing to check it against.
            JsonSchema? expected = _control == null ? Node.AutomationTask?.InputSchema : null;

            foreach (string error in _sampling.Validate(Node, InputMappingJson, expected))
                Errors.Add($"Input mapping : {error}");
        }

        /// <summary>
        /// The mapping with its references replaced by what they point at, which is what the node
        /// hands over. Empty when there is nothing to resolve.
        /// </summary>
        private string Resolve()
        {
            if (string.IsNullOrWhiteSpace(InputMappingJson))
                return string.Empty;

            JToken template;
            try
            {
                template = JToken.Parse(InputMappingJson);
            }
            catch
            {
                // Not JSON yet : the error says it, there is nothing to resolve in the meantime.
                return string.Empty;
            }

            List<string> resolved = [];
            IReadOnlyList<GraphContext> contexts = _sampling.GetContexts(Node);
            foreach (GraphContext context in contexts)
            {
                // Resolved the very way the executor resolves it, only against samples.
                string text = context.Resolve(template)?.ToString(Formatting.Indented) ?? string.Empty;
                resolved.Add(context.Branch == null || contexts.Count == 1
                    ? text
                    : $"// from {context.Branch}{Environment.NewLine}{text}");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, resolved);
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate()
        {
            Edition = BuildEdition();
            _overlays.CloseTop(true);
        }

        private bool CanValidate() => IsEditable && !HasErrors;

        [RelayCommand]
        private void Cancel() => _overlays.CloseTop(false);

        /// <summary>
        /// Leave the node behind and open the settings of the workflow, where the input it hands
        /// over is edited.
        /// </summary>
        [RelayCommand]
        private void OpenWorkflowSettings()
        {
            _overlays.CloseTop(false);
            _openWorkflowSettings?.Invoke();
        }

        /// <summary>
        /// Build the edition of the graph from the current mapping : the value to apply and the one
        /// it replaces, so the editor can undo it. Every node holds its mapping and nothing else,
        /// the schemas being deduced from them when the workflow is saved.
        /// </summary>
        private IReversibleAction BuildEdition()
        {
            string? mapping = NullIfEmpty(InputMappingJson);
            string? previous = Node.InputMappingJson;

            return new ReversibleAction(
                $"Edit the mapping of '{Node.Name}'",
                () => Node.InputMappingJson = mapping,
                () => Node.InputMappingJson = previous);
        }

        /// <summary>
        /// An empty text box means the node maps nothing, which is null rather than "".
        /// </summary>
        private static string? NullIfEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? null : json;

        partial void OnInputMappingJsonChanged(string? value) => Refresh();
    }
}
