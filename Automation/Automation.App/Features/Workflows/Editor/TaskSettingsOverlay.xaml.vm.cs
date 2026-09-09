using System.Collections.ObjectModel;
using Automation.App.Common;
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
    /// the node is reached one way and break when it is reached another. [Branch] is null when the
    /// node is only reached one way and there is nothing to tell apart.
    /// </summary>
    public record MappingError(string Message, string? Branch = null)
    {
        public bool HasBranch => Branch != null;
    }

    /// <summary>
    /// What a node does with its mapping, which is the only thing telling the kinds apart.
    /// </summary>
    public enum EnumNodeKind
    {
        Task,
        Start,
        End,
        Share,
        Join,
        Map,
        Control
    }

    /// <summary>
    /// How a kind of node reads : what it is called, what it does with its mapping, what the shape
    /// in front of the mapping is the shape of, and what to say when that shape describes nothing.
    /// </summary>
    public record NodeKindText(string Name, string Description, string ExpectedLabel, string EmptyExpectation);

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
    public partial class TaskSettingsViewModel : OverlayViewModel<IReversibleAction>
    {
        public BaseGraphTask Node { get; }

        /// <summary>
        /// Mapping the node runs with, references to the context included (e.g. "$previous.Value").
        /// </summary>
        [ObservableProperty] private string? _inputMappingJson;

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

        /// <summary>
        /// What the node is about, displayed above the mapping : every node maps what it reads into
        /// what it hands over, only what is done with the result changes.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The package and class the node's task runs, null when the node is a control, a nested
        /// workflow, or a task without a target yet : none of those have one to summarize.
        /// </summary>
        public PackageClassTarget? Target { get; }

        public bool HasTarget => Target != null;

        /// <summary>
        /// What the mapping stands for : the parameters of a task everywhere, the default values on
        /// the start.
        /// </summary>
        public string MappingLabel { get; }

        /// <summary>
        /// The shape the mapping has to produce, which is what a reader needs in front of them while
        /// writing one. Read from what the node declares : what it reads everywhere, except on the
        /// start whose mapping produces what it hands over instead.
        /// <para>
        /// Editable on the start and the end only : they are the boundary of the workflow, so those
        /// two shapes are what a caller reads it by (see
        /// <see cref="AutomationWorkflow.DeriveSchemas"/>). Everywhere else the shape belongs to the
        /// task the node runs and is only shown.
        /// </para>
        /// </summary>
        [ObservableProperty] private string? _expectedSchemaJson;

        /// <summary>
        /// Whether the expected shape is the node to declare, which the start and the end are alone
        /// in doing.
        /// </summary>
        public bool DeclaresSchema => IsStart || IsEnd;

        /// <summary>
        /// Whether the expected shape is only shown : it belongs to the task the node runs rather
        /// than to the node.
        /// </summary>
        public bool IsExpectedReadOnly => !DeclaresSchema;

        /// <summary>What the expected shape is the shape of.</summary>
        public string ExpectedLabel { get; }

        /// <summary>
        /// What to say when the shape expected of the mapping describes nothing : a control other
        /// than the boundary of the workflow constrains none, and a task can expect nothing too.
        /// </summary>
        public string EmptyExpectationText { get; }

        /// <summary>
        /// The start stands for what the workflow is started with : nothing feeds it, so it reads no
        /// branch, and its mapping holds the values a caller does not give.
        /// </summary>
        public bool IsStart => _kind == EnumNodeKind.Start;

        /// <summary>The end stands for what the workflow hands back.</summary>
        public bool IsEnd => _kind == EnumNodeKind.End;

        /// <summary>What the node does with its mapping, read once from the node it stands for.</summary>
        private readonly EnumNodeKind _kind;

        /// <summary>
        /// The ways a run can reach the node, as the preview of the graph found them : what it reads
        /// and which branches fed it. Empty when the graph could not be walked.
        /// </summary>
        private readonly IReadOnlyList<NodePreviewContext> _contexts;

        public TaskSettingsViewModel(
            BaseGraphTask node,
            GraphExecutionPreview? preview,
            IOverlayService overlays)
            : base(overlays)
        {
            Node = node;
            _kind = KindOf(node as GraphControl);
            _contexts = preview?.NodesContexts.GetValueOrDefault(node.Id) ?? [];

            NodeKindText text = TextOf(_kind);
            Options.Title = $"{node.Name} - {text.Name}";
            Description = text.Description;
            ExpectedLabel = text.ExpectedLabel;
            EmptyExpectationText = text.EmptyExpectation;
            Target = (node.AutomationTask as AutomationTask)?.Target;
            MappingLabel = IsStart ? "Default values, for what the caller leaves out" : "Input mapping";
            _inputMappingJson = node.InputTemplateJson;
            // The mapping of a node produces what that node reads, except on the start : nothing
            // feeds a start, so its mapping produces what it hands over instead.
            _expectedSchemaJson = IsStart ? node.OutputSchemaJson : node.InputSchemaJson;

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
        public static Task<IReversibleAction?> ShowAsync(
            BaseGraphTask node,
            GraphExecutionPreview? preview)
            => ShowAsync(new TaskSettingsViewModel(node, preview, SpineViewModel.Instance.Overlays));

        /// <summary>
        /// What [control] does with its mapping, <see cref="EnumNodeKind.Task"/> when the node runs
        /// a task or a nested workflow rather than a control.
        /// </summary>
        private static EnumNodeKind KindOf(GraphControl? control)
        {
            if (control == null)
                return EnumNodeKind.Task;
            if (control.IsStart())
                return EnumNodeKind.Start;
            if (control.IsEnd())
                return EnumNodeKind.End;
            if (control.IsShare())
                return EnumNodeKind.Share;
            if (control.IsJoin())
                return EnumNodeKind.Join;
            if (control.IsMap())
                return EnumNodeKind.Map;
            return EnumNodeKind.Control;
        }

        /// <summary>
        /// How [kind] reads. A control other than the boundary of the workflow expects no shape :
        /// what it hands over is whatever its mapping says, so it is worth saying that rather than
        /// showing an empty schema without a word.
        /// </summary>
        private static NodeKindText TextOf(EnumNodeKind kind)
        {
            const string controlShape = "A control constrains no shape : what it hands over is whatever its mapping produces.";
            const string controlLabel = "Expected : a control constrains no shape";
            const string reshape = "The mapping reshapes what one branch produces into what the next ones read.";

            return kind switch
            {
                EnumNodeKind.Task => new(
                    "Task",
                    "The mapping is what the task runs with : it has to match what the task expects.",
                    "Expected : the shape the task is run with",
                    "The task expects no particular shape."),
                EnumNodeKind.Start => new(
                    "Start",
                    "The start hands over what the workflow is started with : the schema declares its shape, the mapping holds the values a caller leaves out.",
                    "Expected : the shape the workflow is started with",
                    controlShape),
                EnumNodeKind.End => new(
                    "End",
                    "The mapping is what the workflow hands back to whoever started it, and the schema declares its shape.",
                    "Expected : the shape the workflow hands back",
                    controlShape),
                EnumNodeKind.Share => new(
                    "Share",
                    "The mapping is added to the shared values, readable as \"$shared\" by every node after this one. The branch itself goes through untouched.",
                    controlLabel,
                    controlShape),
                EnumNodeKind.Join => new(
                    "Join",
                    "Every branch reaching the join is waited for, then merged into what the mapping describes.",
                    controlLabel,
                    controlShape),
                EnumNodeKind.Map => new("Map", reshape, controlLabel, controlShape),
                _ => new("Control", reshape, controlLabel, controlShape),
            };
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
                ContextEntry? branch = null;
                if (BranchLabel(context) is string label)
                {
                    branch = ContextEntry.Branch($"from {label}");
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
        /// The branches feeding the node through [context], only worth naming when there is more
        /// than one way in. Null when there is nothing to tell apart.
        /// </summary>
        private string? BranchLabel(NodePreviewContext context)
            => _contexts.Count > 1 && context.Branches.Count > 0 ? string.Join(", ", context.Branches) : null;

        /// <summary>
        /// Check the mapping and show what it produces : both come from resolving it against what
        /// the node reads, so nothing has to be executed to know.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            JsonSchema? expected = CheckSchema();
            CheckMappingJson();

            if (!HasErrors)
                Resolve(expected);
        }

        /// <summary>
        /// The schema the node declares, null when it declares none or when what it holds is not one.
        /// </summary>
        private JsonSchema? CheckSchema()
        {
            // Checked wherever there is a shape to check against, whether the node declares it or
            // takes it from the task it runs : a mapping producing something the task cannot be run
            // with is wrong either way.
            if (string.IsNullOrWhiteSpace(ExpectedSchemaJson))
                return null;

            try
            {
                return Schemas.Parse(ExpectedSchemaJson);
            }
            catch (Exception exception)
            {
                Errors.Add(new MappingError($"Expected shape : {exception.Message}"));
                return null;
            }
        }

        /// <summary>
        /// Check what the mapping resolved to on one branch against the shape expected of it. What a
        /// reference stands for is only known once resolved, so the shape is checked on what came out
        /// rather than on the mapping itself.
        /// <para>
        /// The values of the start only stand for what a caller leaves out, so a property the schema
        /// requires is allowed to be missing from them.
        /// </para>
        /// </summary>
        private void CheckAgainstSchema(JsonSchema expected, JToken resolved, string? branch)
        {
            foreach (ValidationError error in expected.Validate(resolved.ToString(Formatting.None)))
            {
                if (IsStart && error.Kind == ValidationErrorKind.PropertyRequired)
                    continue;
                Errors.Add(new MappingError($"{error.Path} : {error.Kind}", branch));
            }
        }

        /// <summary>
        /// Add an error when the mapping is filled with something that isn't JSON. An empty value is
        /// valid, it simply means the node maps nothing.
        /// </summary>
        private void CheckMappingJson()
        {
            if (string.IsNullOrWhiteSpace(InputMappingJson))
                return;

            try
            {
                JToken.Parse(InputMappingJson);
            }
            catch (Exception exception)
            {
                Errors.Add(new MappingError($"{MappingLabel} : {exception.Message}"));
            }
        }

        /// <summary>
        /// Resolve the mapping against every context the node can be reached with — the very way a
        /// run resolves it — holding against it whatever cannot be resolved, and whatever does not
        /// match [expected]. Resolved once per branch : a mapping can hold up on one and break on
        /// another, and the error says which.
        /// </summary>
        private void Resolve(JsonSchema? expected)
        {
            if (string.IsNullOrWhiteSpace(InputMappingJson))
                return;

            foreach (NodePreviewContext context in _contexts)
            {
                string? branch = BranchLabel(context);

                // Both are parsed again for every context : resolving moves what a reference points
                // at into the mapping, so neither of them survives being resolved twice.
                JToken template = JToken.Parse(InputMappingJson);
                JObject values = (JObject)context.Context.DeepClone();

                ReferenceReplaceResult result = ReferencesHandler.ReplaceReferences(template, values);
                foreach (string error in result.Errors)
                    Errors.Add(new MappingError(error, branch));

                // A mapping still holding references it could not resolve says nothing about its
                // shape : the references left in it would be read as the strings they are written as.
                if (expected != null && !result.HasErrors)
                    CheckAgainstSchema(expected, result.Replaced, branch);
            }
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate() => Close(BuildEdition());

        private bool CanValidate() => !HasErrors;

        /// <summary>
        /// Build the edition of the graph from what was edited : the values to apply and the ones
        /// they replace, so the editor can undo it. A node holds its mapping, the start and the end
        /// holding a schema of the workflow along with it. Its name is edited on the graph itself.
        /// </summary>
        private IReversibleAction BuildEdition()
        {
            string? mapping = Json.NullIfEmpty(InputMappingJson);
            string? previousMapping = Node.InputTemplateJson;

            string? schema = DeclaresSchema ? Json.NullIfEmpty(ExpectedSchemaJson) : null;
            string? previousSchema = DeclaresSchema
                ? (IsStart ? Node.OutputSchemaJson : Node.InputSchemaJson)
                : null;

            return new ReversibleAction(
                $"Edit '{Node.Name}'",
                () => Write(mapping, schema),
                () => Write(previousMapping, previousSchema));
        }

        /// <summary>
        /// Write onto the node what the settings hold : its mapping and, on the boundary of the
        /// workflow, the schema it declares.
        /// </summary>
        private void Write(string? mapping, string? schema)
        {
            Node.InputTemplateJson = mapping;

            if (!DeclaresSchema)
                return;

            if (IsStart)
                Node.OutputSchemaJson = schema;
            else
                Node.InputSchemaJson = schema;
        }

        partial void OnInputMappingJsonChanged(string? value) => Refresh();

        partial void OnExpectedSchemaJsonChanged(string? value) => Refresh();
    }
}
