using System.Collections.ObjectModel;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

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

        /// <summary>
        /// A row standing for one of the ways into the node rather than for a value : it groups what
        /// that branch hands over, so it holds no reference and previews nothing of its own.
        /// </summary>
        public static ContextEntry Branch(string name) => new ContextEntry(name);

        private ContextEntry(string name)
        {
            Name = name;
            Reference = string.Empty;
            Preview = string.Empty;
        }

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
    /// One thing wrong with the mapping, and the branch it is wrong on : a mapping can hold up when
    /// the node is reached one way and break when it is reached another.
    /// </summary>
    public class MappingError
    {
        public string Message { get; }

        /// <summary>
        /// The branches feeding the node where the mapping breaks, null when the node is only
        /// reached one way and there is nothing to tell apart.
        /// </summary>
        public string? Branch { get; }

        public bool HasBranch => Branch != null;

        public MappingError(string message, string? branch = null)
        {
            Message = message;
            Branch = branch;
        }
    }

    /// <summary>
    /// Settings of a graph node : the mapping it runs with, edited as raw JSON between what it reads
    /// (the branches reaching it, the shared values and the context of its scopes) and what comes
    /// out of it once the references are resolved.
    /// <para>
    /// A node is read once per context a run can reach it with (see
    /// <see cref="GraphExecutionPreview"/>), so the mapping is checked against every branch leading
    /// to it rather than against one of them : what is wrong on a single branch says which.
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
        /// What the mapping produces once its references are resolved, one block per branch reaching
        /// the node : the parameters the task would run with, or the values the node hands over.
        /// </summary>
        [ObservableProperty] private string _resultJson = string.Empty;

        /// <summary>
        /// What the node reads, one root per branch reaching it : a reference is written from there.
        /// </summary>
        public ObservableCollection<ContextEntry> Context { get; } = [];

        /// <summary>
        /// What is wrong with the current mapping, blocking the validation while not empty.
        /// </summary>
        public ObservableCollection<MappingError> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Whether nothing is known of what the node reads : the graph could not be walked up to it,
        /// or nothing walked it at all. The mapping is then edited blind, the references having
        /// nothing to be resolved against and so nothing to be checked against either.
        /// </summary>
        public bool IsContextMissing => _contexts.Count == 0;

        public string Title { get; }

        /// <summary>
        /// What the node is about, displayed above the mapping : every node maps what it reads into
        /// what it hands over, only what is done with the result changes.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// What the resolved mapping stands for, which is not the same thing for every kind of node.
        /// </summary>
        public string ResultLabel { get; }

        /// <summary>
        /// What the mapping stands for : the parameters of a task everywhere, the default values on
        /// the start.
        /// </summary>
        public string MappingLabel { get; }

        /// <summary>
        /// The schema the node declares, edited on the start and the end only : they are the boundary
        /// of the workflow, so what one hands over and what the other hands back is what a caller
        /// reads the workflow by (see <see cref="AutomationWorkflow.DeriveSchemas"/>). Null anywhere
        /// else, a node taking the shapes of the task it points at.
        /// </summary>
        [ObservableProperty] private string? _schemaJson;

        /// <summary>Whether the node declares one of the two schemas of the workflow.</summary>
        public bool HasSchema => IsStart || IsEnd;

        /// <summary>What the declared schema is the shape of.</summary>
        public string SchemaLabel { get; }

        /// <summary>
        /// The start stands for what the workflow is started with : nothing feeds it, so it reads no
        /// branch, and its mapping holds the values a caller does not give.
        /// </summary>
        public bool IsStart => _control?.IsStart() == true;

        /// <summary>The end stands for what the workflow hands back.</summary>
        public bool IsEnd => _control?.IsEnd() == true;

        /// <summary>
        /// The node as a control task, null when it is a regular task or a nested workflow.
        /// </summary>
        private readonly GraphControl? _control;

        /// <summary>
        /// The ways a run can reach the node, as the preview of the graph found them : what it reads
        /// and which branches fed it. Empty when the graph could not be walked.
        /// </summary>
        private readonly IReadOnlyList<NodePreviewContext> _contexts;

        private readonly IOverlayService _overlays;

        public TaskSettingsViewModel(
            BaseGraphTask node,
            GraphExecutionPreview? preview,
            IOverlayService overlays)
        {
            Node = node;
            _overlays = overlays;
            _control = node as GraphControl;
            _contexts = preview?.NodesContexts.GetValueOrDefault(node.Id) ?? [];

            Title = $"{node.Name} - {Describe()}";
            Description = Explain();
            ResultLabel = LabelResult();
            MappingLabel = IsStart ? "Default values, for what the caller leaves out" : "Input mapping";
            SchemaLabel = IsStart
                ? "Schema of what the workflow is started with"
                : "Schema of what the workflow hands back";
            _inputMappingJson = node.InputTemplateJson;
            // The start declares what it hands over, the end what it reads : an end produces nothing
            // of its own, so the output of the workflow is the shape reaching it.
            _schemaJson = IsStart ? node.OutputSchemaJson : IsEnd ? node.InputSchemaJson : null;

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
        /// <para>
        /// [preview] is what the editor last found the graph would run into, and where the contexts
        /// the mapping is resolved against come from : previewing the graph again here would be a
        /// second walk of it, and one that can disagree with the one the editor is drawing.
        /// </para>
        /// </summary>
        public static async Task<IReversibleAction?> ShowAsync(
            BaseGraphTask node,
            GraphExecutionPreview? preview)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new TaskSettingsViewModel(node, preview, overlays);
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
                return "The start hands over what the workflow is started with : the schema declares its shape, the mapping holds the values a caller leaves out.";
            if (_control.IsEnd())
                return "The mapping is what the workflow hands back to whoever started it, and the schema declares its shape.";
            if (_control.IsShare())
                return "The mapping is added to the shared values, readable as \"$shared\" by every node after this one. The branch itself goes through untouched.";
            if (_control.IsJoin())
                return "Every branch reaching the join is waited for, then merged into what the mapping describes.";
            return "The mapping reshapes what one branch produces into what the next ones read.";
        }

        private string LabelResult()
        {
            if (_control == null)
                return "Parameters of the task";
            if (_control.IsStart())
                return "What the workflow is started with";
            if (_control.IsEnd())
                return "What the workflow hands back";
            if (_control.IsShare())
                return "Added to the shared values";
            return "What the next nodes read";
        }

        /// <summary>
        /// Build what the node reads : one root per context reaching it, holding "$previous",
        /// "$shared" and "$global" as they would be read from there.
        /// </summary>
        private void LoadContext()
        {
            Context.Clear();

            foreach (NodePreviewContext context in _contexts)
            {
                // The branch is only worth naming when there is more than one way in.
                ContextEntry? branch = null;
                if (_contexts.Count > 1 && context.Branches.Count > 0)
                {
                    branch = ContextEntry.Branch($"from {string.Join(", ", context.Branches)}");
                    Context.Add(branch);
                }

                foreach (JProperty property in context.Context.Properties())
                {
                    // Nothing runs before the start, so it has no branch and no shared value to read.
                    if (IsStart && property.Name != "global")
                        continue;

                    var entry = new ContextEntry($"${property.Name}", $"${property.Name}", property.Value);
                    if (branch != null)
                        branch.Children.Add(entry);
                    else
                        Context.Add(entry);
                }
            }
        }

        /// <summary>
        /// Check the mapping and show what it produces : both come from resolving it against what
        /// the node reads, so nothing has to be executed to know.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            JsonSchema? declared = CheckSchema();
            CheckJson(MappingLabel, InputMappingJson);
            ResultJson = HasErrors ? string.Empty : Resolve();

            if (declared != null)
                CheckAgainstSchema(declared);
        }

        /// <summary>
        /// The schema the node declares, null when it declares none or when what it holds is not one.
        /// </summary>
        private JsonSchema? CheckSchema()
        {
            if (!HasSchema || string.IsNullOrWhiteSpace(SchemaJson))
                return null;

            try
            {
                return JsonSchema.FromJsonAsync(SchemaJson).Result;
            }
            catch (Exception exception)
            {
                Errors.Add(new MappingError($"{SchemaLabel} : {exception.Message}"));
                return null;
            }
        }

        /// <summary>
        /// Check what the mapping resolved to against the schema the node declares. The two are
        /// edited side by side here, which is what makes it the one place a shape can be checked
        /// against what fills it.
        /// <para>
        /// The values of the start only stand for what a caller leaves out, so a property the schema
        /// requires is allowed to be missing from them.
        /// </para>
        /// </summary>
        private void CheckAgainstSchema(JsonSchema declared)
        {
            if (string.IsNullOrWhiteSpace(ResultJson))
                return;

            foreach (ValidationError error in declared.Validate(ResultJson))
            {
                if (IsStart && error.Kind == ValidationErrorKind.PropertyRequired)
                    continue;
                Errors.Add(new MappingError($"{error.Path} : {error.Kind}"));
            }
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
                Errors.Add(new MappingError($"{label} : {exception.Message}"));
            }
        }

        /// <summary>
        /// The mapping with its references replaced by what they point at, one block per branch
        /// reaching the node, filling <see cref="Errors"/> with whatever it cannot resolve. Resolved
        /// the very way a run resolves it, only against samples.
        /// </summary>
        private string Resolve()
        {
            if (string.IsNullOrWhiteSpace(InputMappingJson))
                return string.Empty;

            List<string> resolved = [];
            foreach (NodePreviewContext context in _contexts)
            {
                string? branch = _contexts.Count > 1 && context.Branches.Count > 0
                    ? string.Join(", ", context.Branches)
                    : null;

                // Both are parsed again for every context : resolving moves what a reference points
                // at into the mapping, so neither of them survives being resolved twice.
                JToken template = JToken.Parse(InputMappingJson);
                JObject values = (JObject)context.Context.DeepClone();

                ReferenceReplaceResult result = ReferencesHandler.ReplaceReferences(template, values);
                foreach (string error in result.Errors)
                    Errors.Add(new MappingError(error, branch));

                string text = result.Replaced.ToString(Formatting.Indented);
                resolved.Add(branch == null ? text : $"// from {branch}{Environment.NewLine}{text}");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, resolved);
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate()
        {
            Edition = BuildEdition();
            _overlays.CloseTop(true);
        }

        private bool CanValidate() => !HasErrors;

        [RelayCommand]
        private void Cancel() => _overlays.CloseTop(false);

        /// <summary>
        /// Build the edition of the graph from what was edited : the values to apply and the ones
        /// they replace, so the editor can undo it. Every node holds its mapping, the start and the
        /// end holding a schema of the workflow along with it.
        /// </summary>
        private IReversibleAction BuildEdition()
        {
            string? mapping = NullIfEmpty(InputMappingJson);
            string? previousMapping = Node.InputTemplateJson;

            if (!HasSchema)
            {
                return new ReversibleAction(
                    $"Edit the mapping of '{Node.Name}'",
                    () => Node.InputTemplateJson = mapping,
                    () => Node.InputTemplateJson = previousMapping);
            }

            string? schema = NullIfEmpty(SchemaJson);
            string? previousSchema = IsStart ? Node.OutputSchemaJson : Node.InputSchemaJson;

            return new ReversibleAction(
                $"Edit '{Node.Name}'",
                () =>
                {
                    Node.InputTemplateJson = mapping;
                    Declare(schema);
                },
                () =>
                {
                    Node.InputTemplateJson = previousMapping;
                    Declare(previousSchema);
                });
        }

        /// <summary>
        /// Write [schema] where the node declares it : what the start hands over, what the end reads.
        /// </summary>
        private void Declare(string? schema)
        {
            if (IsStart)
                Node.OutputSchemaJson = schema;
            else
                Node.InputSchemaJson = schema;
        }

        /// <summary>
        /// An empty text box means the node maps nothing, which is null rather than "".
        /// </summary>
        private static string? NullIfEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? null : json;

        partial void OnInputMappingJsonChanged(string? value) => Refresh();

        partial void OnSchemaJsonChanged(string? value) => Refresh();
    }
}
