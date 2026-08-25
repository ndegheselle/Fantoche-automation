using System.Collections.ObjectModel;
using System.Windows;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// What a node of the graph holds its settings for : a regular task runs with its own
    /// parameters, while the control tasks stand for something the workflow itself holds.
    /// </summary>
    public enum EnumTaskSettingsKind
    {
        /// <summary>A task or a nested workflow : its own JSON parameters.</summary>
        Task,

        /// <summary>The start of the graph : the input schema of the workflow.</summary>
        Start,

        /// <summary>An end of the graph : the output mapping and schema of the workflow.</summary>
        End,

        /// <summary>A context setter : the values it puts in the context of its branch.</summary>
        Context
    }

    /// <summary>
    /// Settings of a graph node, everything being edited as raw JSON : the context the task reads
    /// (read only, inferred from the previous tasks), what the node holds, and the resulting schema
    /// (read only).
    /// <para>
    /// The settings are only written to the graph once validated, and as a
    /// <see cref="IReversibleAction"/> handed over to the editor : nothing is edited in place, so
    /// cancelling has nothing to restore and saving stays the editor's business.
    /// </para>
    /// </summary>
    public partial class TaskSettingsViewModel : ObservableObject
    {
        public BaseGraphTask Node { get; }

        public AutomationWorkflow Workflow { get; }

        public EnumTaskSettingsKind Kind { get; }

        /// <summary>
        /// Edition to apply to the graph, only set once the settings have been validated.
        /// </summary>
        public IReversibleAction? Edition { get; private set; }

        /// <summary>
        /// Samples of the context the task reads, one per branch reaching it : the output of the
        /// previous tasks under <c>previous</c> and the context of the workflow under <c>context</c>.
        /// </summary>
        [ObservableProperty] private List<string> _contextSamples = [];

        /// <summary>
        /// What the node holds, as edited by the user : the task parameters, the sample of the
        /// workflow input, its output mapping or the values set in the context.
        /// </summary>
        [ObservableProperty] private string? _settingsJson;

        /// <summary>
        /// Schema (or resulting context) matching the settings, displayed read only.
        /// </summary>
        [ObservableProperty] private string? _schemaJson;

        /// <summary>
        /// Whether the task waits for every branch reaching it before running, which changes the
        /// shape of the context it reads : the outputs are then keyed by node name.
        /// </summary>
        [ObservableProperty] private bool _isWaitingAllInputs;

        /// <summary>
        /// What is wrong with the current settings, blocking the validation while not empty.
        /// </summary>
        public ObservableCollection<string> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Whether the node reads a context at all : the start of the graph, or a node nothing is
        /// connected to, has none to display.
        /// </summary>
        public bool HasContext => ContextSamples.Count > 0;

        /// <summary>
        /// Width of the context column, taken back by the settings when there is no context to show.
        /// </summary>
        public GridLength ContextWidth => HasContext ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        public string Title { get; }

        public string Hint { get; }

        public string ContextLabel { get; }

        public string SettingsLabel { get; }

        public string SchemaLabel { get; }

        /// <summary>
        /// Whether the wait-for-all-inputs setting applies : every node but the start of the graph,
        /// which has no input.
        /// </summary>
        public bool CanWaitAllInputs => Kind != EnumTaskSettingsKind.Start;

        /// <summary>
        /// Input schema the parameters are validated against, parsed once.
        /// </summary>
        private readonly JsonSchema? _inputSchema;

        /// <summary>
        /// Context read by a context setter, its resulting context being previewed from it.
        /// </summary>
        private readonly JToken? _incomingContext;

        private readonly IOverlayService _overlays;

        public TaskSettingsViewModel(BaseGraphTask node, AutomationWorkflow workflow, IOverlayService overlays)
        {
            Node = node;
            Workflow = workflow;
            _overlays = overlays;
            Kind = GetKind(node);

            _isWaitingAllInputs = node.Settings.IsWaitingAllInputs;
            _inputSchema = Kind == EnumTaskSettingsKind.Task ? node.InputSchema : null;
            _incomingContext = Kind == EnumTaskSettingsKind.Context
                ? workflow.Graph.Execution.GetContextSampleFor(node)
                : null;

            (Title, Hint, ContextLabel, SettingsLabel, SchemaLabel) = Kind switch
            {
                EnumTaskSettingsKind.Start => (
                    $"{node.Name} - workflow input",
                    "The schema of the workflow input is inferred from the sample : whatever is written here is accepted when the workflow runs.",
                    "Context",
                    "Input sample",
                    "Inferred input schema"),
                EnumTaskSettingsKind.End => (
                    $"{node.Name} - workflow output",
                    "Every end task acts as one : their previous tasks are mixed together and they all share this mapping.",
                    "Context of the ends",
                    "Output mapping",
                    "Inferred output schema"),
                EnumTaskSettingsKind.Context => (
                    $"{node.Name} - context values",
                    "A value overrides what the context carries, a null one leaves it untouched and a new key is added to it.",
                    "Context read",
                    "Context values",
                    "Resulting context"),
                _ => (
                    $"{node.Name} - settings",
                    "Reference the context with '$', e.g. \"$previous.Value\" or \"$context.errorMail\".",
                    "Context read",
                    "Parameters",
                    "Output schema")
            };

            Errors.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasErrors));
                ValidateCommand.NotifyCanExecuteChanged();
            };

            // A regular task doesn't infer anything, its output schema comes from the task itself
            if (Kind == EnumTaskSettingsKind.Task)
                _schemaJson = node.OutputSchemaJson;

            _settingsJson = InitialSettingsJson();
            RefreshSamples();
            Refresh();
        }

        /// <summary>
        /// What the node holds its settings for : the control tasks stand for the workflow input,
        /// output or context, anything else runs with its own parameters.
        /// </summary>
        private static EnumTaskSettingsKind GetKind(BaseGraphTask node)
        {
            if (node is not GraphControl control)
                return EnumTaskSettingsKind.Task;
            if (control.IsStart())
                return EnumTaskSettingsKind.Start;
            if (control.IsEnd())
                return EnumTaskSettingsKind.End;
            if (control.IsShare())
                return EnumTaskSettingsKind.Context;
            return EnumTaskSettingsKind.Task;
        }

        /// <summary>
        /// Show the settings of [node] and wait for the user to validate them, the edition to apply
        /// to the graph being returned (<see langword="null"/> when cancelled).
        /// </summary>
        public static async Task<IReversibleAction?> ShowAsync(BaseGraphTask node, AutomationWorkflow workflow)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new TaskSettingsViewModel(node, workflow, overlays);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = viewModel.Title }) != true)
                return null;
            return viewModel.Edition;
        }

        /// <summary>
        /// Settings the overlay opens on : what the node already holds, or a starting point built
        /// from what it expects.
        /// </summary>
        private string? InitialSettingsJson()
        {
            switch (Kind)
            {
                case EnumTaskSettingsKind.Start:
                    return Workflow.InputSchema?.ToSampleJson().ToString() ?? "{}";
                case EnumTaskSettingsKind.End:
                    return Workflow.OutputMappingJson ?? "{}";
                case EnumTaskSettingsKind.Context:
                    // Every key the context carries at that point, unset : the user only fills the
                    // ones the branch overrides.
                    return Node.ParametersJson
                        ?? Workflow.Graph.Execution.GetContextSetterDefaultSettings(Node).ToString();
                default:
                    return Node.ParametersJson ?? _inputSchema?.ToSampleJson().ToString() ?? "{}";
            }
        }

        /// <summary>
        /// Rebuild the context samples, the shape of the context depending on whether the task waits
        /// for all its inputs.
        /// </summary>
        private void RefreshSamples()
        {
            ContextSamples = Kind == EnumTaskSettingsKind.End
                ? Workflow.Graph.Execution.GetContextSampleForEnd(IsWaitingAllInputs)
                : Workflow.Graph.Execution.GetContextSampleJsonFor(Node, IsWaitingAllInputs);
        }

        /// <summary>
        /// Check the settings against the context they read and refresh what is inferred from them :
        /// the schema of the workflow input / output, or the resulting context of a context setter.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            if (string.IsNullOrWhiteSpace(SettingsJson))
            {
                Errors.Add("The settings can't be empty.");
                return;
            }

            // No sample at all (the start of the graph) still goes through one pass, references
            // simply having nothing to point at.
            IEnumerable<string?> samples = ContextSamples.Count > 0 ? ContextSamples : [null];

            MultiReferenceReplaceContext replaced;
            try
            {
                replaced = ReferencesHandler.ReplaceReferences(SettingsJson, samples);
            }
            catch (Exception exception)
            {
                Errors.Add($"The JSON is not valid : {exception.Message}");
                return;
            }

            foreach (var error in replaced.InconsistentReferenceErrors)
                Errors.Add(error.ToString());
            foreach (string error in replaced.Contexts.SelectMany(x => x.Errors).Select(x => x.ToString()).Distinct())
                Errors.Add(error);

            JToken? resolved = replaced.Contexts.FirstOrDefault()?.ReplacedSetting;
            if (resolved == null)
                return;

            try
            {
                switch (Kind)
                {
                    case EnumTaskSettingsKind.Start:
                    case EnumTaskSettingsKind.End:
                        // The schema is what the sample / mapping describes, the user never writes it
                        SchemaJson = JsonSchema.FromSampleJson(resolved.ToString()).ToJson();
                        break;
                    case EnumTaskSettingsKind.Context:
                        SchemaJson = GraphExecutionContext.ApplyContextSetter(_incomingContext, resolved).ToString();
                        break;
                    default:
                        // The parameters have to match the schema of the task once resolved, whichever
                        // branch reaches it
                        foreach (var context in replaced.Contexts)
                            foreach (var error in _inputSchema?.Validate(context.ReplacedSetting) ?? [])
                                Errors.Add(error.ToString());
                        break;
                }
            }
            catch (Exception exception)
            {
                Errors.Add(exception.Message);
            }
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
        /// Build the edition of the graph from the current settings : the values to apply and the
        /// ones they replace, so the editor can undo it.
        /// </summary>
        private IReversibleAction BuildEdition()
        {
            string? settings = SettingsJson;
            string? schema = SchemaJson;
            bool waitAll = IsWaitingAllInputs;

            switch (Kind)
            {
                case EnumTaskSettingsKind.Start:
                {
                    // Every start node hands the workflow input over, its output schema is that input
                    List<GraphControl> starts = [.. Workflow.Graph.GetStartNodes()];
                    string? previousSchema = Workflow.InputSchemaJson;
                    List<string?> previousOutputs = [.. starts.Select(x => x.OutputSchemaJson)];

                    return new ReversibleAction(
                        $"Edit the input of '{Workflow.Metadata.Name}'",
                        () =>
                        {
                            Workflow.InputSchemaJson = schema;
                            foreach (GraphControl start in starts)
                                start.OutputSchemaJson = schema;
                        },
                        () =>
                        {
                            Workflow.InputSchemaJson = previousSchema;
                            foreach ((GraphControl start, string? output) in starts.Zip(previousOutputs))
                                start.OutputSchemaJson = output;
                        });
                }
                case EnumTaskSettingsKind.End:
                {
                    // The ends act as one : the mapping and the wait setting are shared by all of them
                    List<GraphControl> ends = [.. Workflow.Graph.GetEndNodes()];
                    string? previousSchema = Workflow.OutputSchemaJson;
                    string? previousMapping = Workflow.OutputMappingJson;
                    List<(string? Parameters, bool WaitAll)> previousEnds =
                        [.. ends.Select(x => (x.ParametersJson, x.Settings.IsWaitingAllInputs))];

                    return new ReversibleAction(
                        $"Edit the output of '{Workflow.Metadata.Name}'",
                        () =>
                        {
                            Workflow.OutputSchemaJson = schema;
                            Workflow.OutputMappingJson = settings;
                            foreach (GraphControl end in ends)
                            {
                                end.ParametersJson = settings;
                                end.Settings.IsWaitingAllInputs = waitAll;
                            }
                        },
                        () =>
                        {
                            Workflow.OutputSchemaJson = previousSchema;
                            Workflow.OutputMappingJson = previousMapping;
                            foreach ((GraphControl end, (string? parameters, bool endWaitAll)) in ends.Zip(previousEnds))
                            {
                                end.ParametersJson = parameters;
                                end.Settings.IsWaitingAllInputs = endWaitAll;
                            }
                        });
                }
                default:
                {
                    string? previousSettings = Node.ParametersJson;
                    bool previousWaitAll = Node.Settings.IsWaitingAllInputs;

                    return new ReversibleAction(
                        $"Edit the settings of '{Node.Name}'",
                        () =>
                        {
                            Node.ParametersJson = settings;
                            Node.Settings.IsWaitingAllInputs = waitAll;
                        },
                        () =>
                        {
                            Node.ParametersJson = previousSettings;
                            Node.Settings.IsWaitingAllInputs = previousWaitAll;
                        });
                }
            }
        }

        partial void OnContextSamplesChanged(List<string> value)
        {
            OnPropertyChanged(nameof(HasContext));
            OnPropertyChanged(nameof(ContextWidth));
        }

        partial void OnSettingsJsonChanged(string? value) => Refresh();

        partial void OnIsWaitingAllInputsChanged(bool value)
        {
            RefreshSamples();
            Refresh();
        }
    }
}
