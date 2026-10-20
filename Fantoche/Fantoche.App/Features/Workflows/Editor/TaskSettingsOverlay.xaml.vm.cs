using System.Collections.ObjectModel;
using Fantoche.App.Features.Workflows.Editor.History;
using Fantoche.Shared.Data;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Fantoche.App.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Data.Model;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.App.Features.Workflows.Editor
{
    /// <summary>
    /// The labels the overlay shows for a kind of node.
    /// </summary>
    public record NodeKindTexts(string MappingLabel, string ExpectedLabel, string EmptyExpectation);

    /// <summary>
    /// Settings of a graph node : the mapping it runs with, edited between what the node reads and
    /// the shape expected of it. The mapping is checked by <see cref="MappingCheck"/>.
    /// <para>
    /// Nothing is edited in place : the settings are handed to the editor as an
    /// <see cref="IReversibleAction"/>, so cancelling has nothing to restore and undo works.
    /// </para>
    /// </summary>
    public partial class TaskSettingsViewModel : ObservableObject, IOverlayContent<IReversibleAction>
    {
        /// <summary>
        /// Only the start and the end use their mapping differently : the other controls differ
        /// from each other by name only.
        /// </summary>
        private enum EnumNodeKind
        {
            Task,
            Start,
            End,
            Control
        }

        public OverlayOptions Options { get; }

        public IReversibleAction? Result => BuildEdition();

        public NodeKindTexts Texts { get; }

        /// <summary>
        /// Mapping the node runs with, references to the context included (e.g. "$previous.Value").
        /// </summary>
        public DataObject Mapping { get; }

        /// <summary>
        /// Whether the mapping is shaped by the task's schema, the user only filling in values.
        /// Otherwise the user builds its shape.
        /// </summary>
        public bool IsFilled { get; }

        /// <summary>
        /// What a field of the mapping can be forced to.
        /// </summary>
        public IReadOnlyList<DataManualValue> References { get; }

        /// <summary>
        /// What the node reads, under one property per branch when more than one reaches it.
        /// </summary>
        public DataObject Context { get; }

        /// <summary>
        /// What is wrong with the current mapping, blocking the validation while not empty.
        /// </summary>
        public ObservableCollection<MappingError> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Without a preview of the graph, references can be neither resolved nor checked.
        /// </summary>
        public bool IsContextMissing => _contexts.Count == 0;

        /// <summary>
        /// The package and class the node's task runs, null when the node has none (a control, a
        /// nested workflow, or a task without a target yet).
        /// </summary>
        public PackageClassTarget? Target { get; }

        public bool HasTarget => Target != null;

        /// <summary>
        /// The shape the mapping has to produce, read only. Null when the schema describes no
        /// object, <see cref="NodeKindTexts.EmptyExpectation"/> being displayed instead.
        /// </summary>
        [ObservableProperty] private DataObject? _expectedNode;

        private string? _expectedSchemaJson;

        private readonly BaseGraphTask _node;
        private readonly EnumNodeKind _kind;
        private readonly IOverlayer _overlayer;

        /// <summary>
        /// The ways a run can reach the node, empty when the graph could not be walked.
        /// </summary>
        private readonly IReadOnlyList<NodePreviewContext> _contexts;

        private bool IsStart => _kind == EnumNodeKind.Start;

        /// <summary>
        /// The start and the end are the boundary of the workflow : their schema is deduced from
        /// their mapping (see <see cref="AutomationWorkflow.DeriveSchemas"/>) instead of belonging to
        /// a task.
        /// </summary>
        private bool DeclaresSchema => _kind is EnumNodeKind.Start or EnumNodeKind.End;

        /// <summary>
        /// The schema the node holds for its mapping. Nothing feeds the start, so its mapping
        /// produces what it hands over rather than what it reads.
        /// </summary>
        private string? NodeSchemaJson => IsStart ? _node.OutputSchemaJson : _node.InputSchemaJson;

        public TaskSettingsViewModel(
            BaseGraphTask node,
            GraphExecutionPreview? preview,
            IOverlayer overlayer)
        {
            _node = node;
            _overlayer = overlayer;
            _kind = KindOf(node);
            _contexts = preview?.NodesContexts.GetValueOrDefault(node.Id) ?? [];

            string kindName = AutomationControl.Get(node.TaskId)?.Metadata.Name ?? _kind.ToString();
            Options = new OverlayOptions() { Title = $"{node.Name} - {kindName}" };
            Texts = TextsOf(_kind);
            Target = (node.AutomationTask as AutomationTask)?.Target;

            _expectedSchemaJson = NodeSchemaJson;
            _expectedNode = FromSchema(_expectedSchemaJson);

            JObject context = ContextOf();
            Context = (DataObject)context.ToDataNode();

            // A second tree from the same schema : this one is edited, the expected one is not.
            DataObject? shapedBySchema = FromSchema(_expectedSchemaJson);
            IsFilled = shapedBySchema != null && !DeclaresSchema;

            JToken? template = Json.Parse(node.InputTemplateJson);
            Mapping = shapedBySchema ?? TreeOf(template);

            List<DataManualValue> readReferences = ReadReferences();
            if (template != null)
                LoadTemplate(template, readReferences);
            TypeLoadedReferences();
            IEnumerable<DataManualValue> heldReferences = HeldReferences().Except(readReferences);
            References = [.. readReferences, .. heldReferences];

            Errors.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasErrors));
                ValidateCommand.NotifyCanExecuteChanged();
            };
            Mapping.Changed += (_, _) => Refresh();

            Refresh();
        }

        /// <summary>
        /// Show the settings of [node] and return the edition to apply to the graph, null when
        /// cancelled.
        /// <para>
        /// [preview] is the editor's last walk of the graph : walking it again here could disagree
        /// with what the editor is drawing.
        /// </para>
        /// </summary>
        public static Task<IReversibleAction?> ShowAsync(BaseGraphTask node, GraphExecutionPreview? preview)
        {
            IOverlayer overlayer = SpineViewModel.Instance.Overlayer;
            var settings = new TaskSettingsViewModel(node, preview, overlayer);
            return overlayer.ShowAsync(settings);
        }

        private static EnumNodeKind KindOf(BaseGraphTask node)
        {
            if (node is not GraphControl control)
                return EnumNodeKind.Task;
            if (control.IsStart())
                return EnumNodeKind.Start;
            if (control.IsEnd())
                return EnumNodeKind.End;
            return EnumNodeKind.Control;
        }

        private static NodeKindTexts TextsOf(EnumNodeKind kind)
        {
            const string boundaryExpectation = "Add properties to the expected object to declare its shape.";

            return kind switch
            {
                EnumNodeKind.Start => new(
                    "Expected object, its values being the defaults for what the caller leaves out",
                    "Input schema",
                    boundaryExpectation),
                EnumNodeKind.End => new(
                    "Expected object, what the workflow hands back",
                    "Output schema",
                    boundaryExpectation),
                // Worth saying rather than showing an empty schema without a word.
                EnumNodeKind.Control => new(
                    "Input mapping",
                    "Dynamic schema",
                    "A control constrains no shape : what it hands over is whatever its mapping produces."),
                _ => new(
                    "Input mapping",
                    "Schema of the task",
                    "The task expects no particular shape."),
            };
        }

        private JObject ContextOf()
        {
            var context = new JObject();
            foreach (NodePreviewContext reached in _contexts)
            {
                JObject readable = Readable(reached);
                if (MappingCheck.BranchLabel(reached, _contexts) is string branch)
                    context[$"from {branch}"] = readable;
                else
                    context.Merge(readable);
            }
            return context;
        }

        /// <summary>
        /// A copy of what [context] lets the node read : nothing runs before the start, so it has no
        /// branch and no shared value to read.
        /// </summary>
        private JObject Readable(NodePreviewContext context)
            => new(context.Context.Properties().Where(property => !IsStart || property.Name == "global"));

        /// <summary>
        /// A tree shaped after [template], used when there is no schema to shape the mapping after.
        /// </summary>
        private static DataObject TreeOf(JToken? template)
        {
            if (template is JObject values && values.ToDataNode() is DataObject tree)
                return tree;
            return new DataObject(null);
        }

        private void LoadTemplate(JToken template, List<DataManualValue> readReferences)
        {
            // Loaded untyped, the references the template holds fit whatever field they are in
            // rather than being loaded as the literal text they are written as.
            IEnumerable<DataManualValue> untypedReferences = Json.ReferencesIn(template)
                .Distinct()
                .Select(reference => new DataManualValue(null, reference));
            Mapping.Load(template, [.. readReferences, .. untypedReferences]);
        }

        /// <summary>
        /// The references to every value the node reads, typed by what they hold so a field is only
        /// offered what fits it. A reference holding a different type on another branch is offered
        /// once per type ; one holding null is left out : untyped, it would be offered to every field.
        /// </summary>
        private List<DataManualValue> ReadReferences()
            => [.. _contexts
                .SelectMany(context => Readable(context).Properties())
                .SelectMany(property => ReferencesTo(property.Value, $"${property.Name}"))
                .Where(entry => entry.Type != null)
                .Distinct()];

        /// <summary>
        /// [reference], then the reference of every value under it : an array is referenced whole.
        /// </summary>
        private static IEnumerable<DataManualValue> ReferencesTo(JToken value, string reference)
        {
            var entry = new DataManualValue(TypeOf(value), reference);
            if (value is not JObject values)
                return [entry];

            return values.Properties()
                .SelectMany(property => ReferencesTo(property.Value, $"{reference}.{property.Name}"))
                .Prepend(entry);
        }

        private static EnumDataType? TypeOf(JToken value) => value.Type switch
        {
            JTokenType.Object => EnumDataType.Object,
            JTokenType.Array => EnumDataType.Array,
            JTokenType.Integer => EnumDataType.Integer,
            JTokenType.Float => EnumDataType.Number,
            JTokenType.Boolean => EnumDataType.Boolean,
            JTokenType.Date => EnumDataType.DateTime,
            JTokenType.TimeSpan => EnumDataType.TimeSpan,
            JTokenType.Null or JTokenType.Undefined => null,
            _ => EnumDataType.String,
        };

        /// <summary>
        /// Give each reference the template loaded untyped the type of the field holding it : left
        /// untyped, it would be offered to every field.
        /// </summary>
        private void TypeLoadedReferences()
        {
            foreach (DataNode node in NodesOf(Mapping))
            {
                if (node is not { IsManual: true, ManualEntry: { Type: null, Value: string reference } } || !reference.StartsWith('$'))
                    continue;

                node.ManualEntry = new DataManualValue(node.Type, reference);
            }
        }

        /// <summary>
        /// The references the mapping holds, kept on offer even when the context no longer has them
        /// so they are not lost on the next edit.
        /// </summary>
        private IEnumerable<DataManualValue> HeldReferences()
            => NodesOf(Mapping)
                .Where(node => node is { IsManual: true, ManualEntry.Value: string reference } && reference.StartsWith('$'))
                .Select(node => node.ManualEntry!);

        /// <summary>
        /// The tree of the object [json] describes, null when it describes none. A schema that can't
        /// be read is reported by <see cref="ParseTaskSchema"/>.
        /// </summary>
        private static DataObject? FromSchema(string? json)
        {
            try
            {
                JsonSchema? schema = Schemas.Parse(json)?.ActualSchema;
                return schema?.IsObject == true ? schema.ToDataNode() as DataObject : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Refresh()
        {
            Errors.Clear();

            JsonSchema? expected = DeclaresSchema ? DeclareSchemaFromMapping() : ParseTaskSchema();
            AddDuplicateKeyErrors();
            if (HasErrors)
                return;

            JToken mapping = Mapping.ToToken()!;
            List<MappingError> errors = MappingCheck.Check(mapping, _contexts, expected, allowMissingRequired: IsStart);
            foreach (MappingError error in errors)
                Errors.Add(error);
        }

        /// <summary>
        /// Deduce the schema the start or the end declares from its mapping, and display it. Null
        /// while the mapping holds nothing, which declares nothing.
        /// </summary>
        private JsonSchema? DeclareSchemaFromMapping()
        {
            JsonSchema? schema = Mapping.Properties.Count > 0 ? Mapping.ToJsonSchema() : null;
            string? schemaJson = schema?.ToJson();

            // Most edits change a value, not the shape : rebuilding the displayed tree for those
            // would redraw it on every keystroke.
            if (schemaJson != _expectedSchemaJson)
            {
                _expectedSchemaJson = schemaJson;
                ExpectedNode = FromSchema(schemaJson);
            }
            return schema;
        }

        private JsonSchema? ParseTaskSchema()
        {
            if (string.IsNullOrWhiteSpace(_expectedSchemaJson))
                return null;

            try
            {
                return Schemas.Parse(_expectedSchemaJson);
            }
            catch (Exception exception)
            {
                Errors.Add(new MappingError($"Expected shape : {exception.Message}"));
                return null;
            }
        }

        /// <summary>
        /// A key used twice in the same object is an error : only one of them would be written.
        /// </summary>
        private void AddDuplicateKeyErrors()
        {
            foreach (DataNode node in NodesOf(Mapping))
            {
                foreach (string error in node.GetErrors(nameof(DataNode.Key)).OfType<string>())
                    Errors.Add(new MappingError(error));
            }
        }

        private static IEnumerable<DataNode> NodesOf(DataNode node)
            => node is IDataParent parent ? parent.Children.SelectMany(NodesOf).Prepend(node) : [node];

        [RelayCommand(CanExecute = nameof(CanValidate))]
        private void Validate() => _overlayer.Validate(this);

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        private bool CanValidate() => !HasErrors;

        /// <summary>
        /// The edition holds both the new and the previous values, so the editor can undo it. The
        /// node's name is not part of it : it is edited on the graph itself.
        /// </summary>
        private IReversibleAction BuildEdition()
        {
            string mapping = Mapping.ToToken()!.ToString(Formatting.Indented);
            string? previousMapping = _node.InputTemplateJson;
            string? schema = _expectedSchemaJson;
            string? previousSchema = NodeSchemaJson;

            return new ReversibleAction(
                $"Edit '{_node.Name}'",
                () => Write(mapping, schema),
                () => Write(previousMapping, previousSchema));
        }

        private void Write(string? mapping, string? schema)
        {
            _node.InputTemplateJson = mapping;

            // A task's schema belongs to the task, only the boundary of the workflow declares its own.
            if (!DeclaresSchema)
                return;

            if (IsStart)
                _node.OutputSchemaJson = schema;
            else
                _node.InputSchemaJson = schema;
        }
    }
}
