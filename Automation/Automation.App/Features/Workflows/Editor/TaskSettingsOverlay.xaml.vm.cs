using System.Collections.ObjectModel;
using Automation.App.Features.Workflows.Editor.History;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json.Linq;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// Settings of a graph node, edited as raw JSON : what it reads (input), what it runs with
    /// (parameters) and what it produces (output). A regular task only owns its parameters, while
    /// the control tasks stand for something the workflow itself holds : whatever the node doesn't
    /// own is displayed read only.
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

        /// <summary>
        /// Edition to apply to the graph, only set once the settings have been validated.
        /// </summary>
        public IReversibleAction? Edition { get; private set; }

        /// <summary>
        /// Schema of what the node reads : its own for a task, the input of the workflow for a start.
        /// </summary>
        [ObservableProperty] private string? _inputJson;

        /// <summary>
        /// Template the node runs with, references to the context included (e.g. "$previous.Value").
        /// </summary>
        [ObservableProperty] private string? _parametersJson;

        /// <summary>
        /// Schema of what the node produces : its own for a task or a join, the output of the
        /// workflow for an end, the shared context for a share.
        /// </summary>
        [ObservableProperty] private string? _outputJson;

        /// <summary>
        /// What is wrong with the current settings, blocking the validation while not empty.
        /// </summary>
        public ObservableCollection<string> Errors { get; } = [];

        public bool HasErrors => Errors.Count > 0;

        public string Title { get; }

        /// <summary>
        /// Only the start owns what it reads : it stands for the input of the workflow.
        /// </summary>
        public bool IsInputReadOnly => _control?.IsStart() != true;

        /// <summary>
        /// Everything but the start runs with parameters, the start only handing the input over.
        /// </summary>
        public bool IsParametersReadOnly => _control?.IsStart() == true;

        /// <summary>
        /// A task produces what its package declares and a start what the workflow is started with :
        /// in both cases the output isn't the node's to write.
        /// </summary>
        public bool IsOutputReadOnly => _control == null || _control.IsStart();

        /// <summary>
        /// The node as a control task, null when it is a regular task or a nested workflow.
        /// </summary>
        private readonly GraphControl? _control;

        private readonly IOverlayService _overlays;

        public TaskSettingsViewModel(BaseGraphTask node, AutomationWorkflow workflow, IOverlayService overlays)
        {
            Node = node;
            Workflow = workflow;
            _overlays = overlays;
            _control = node as GraphControl;
            Title = $"Parameters - {node.Name}";

            _inputJson = node.InputSchemaJson;
            _parametersJson = node.ParametersJson;
            _outputJson = node.OutputSchemaJson;

            // The controls stand for the workflow itself, they display what it holds
            if (_control?.IsStart() == true)
            {
                _inputJson = Workflow.InputSchemaJson;
                _outputJson = Workflow.InputSchemaJson;
            }
            else if (_control?.IsEnd() == true)
                _outputJson = Workflow.OutputSchemaJson;
            else if (_control?.IsShare() == true)
                _outputJson = Workflow.SharedSchemaJson;

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
        public static async Task<IReversibleAction?> ShowAsync(BaseGraphTask node, AutomationWorkflow workflow)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new TaskSettingsViewModel(node, workflow, overlays);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = viewModel.Title }) != true)
                return null;
            return viewModel.Edition;
        }

        /// <summary>
        /// Check that everything the node owns is valid JSON, the validation being blocked while it
        /// isn't.
        /// </summary>
        private void Refresh()
        {
            Errors.Clear();

            if (!IsInputReadOnly)
                CheckJson("Input", InputJson);
            if (!IsParametersReadOnly)
                CheckJson("Parameters", ParametersJson);
            if (!IsOutputReadOnly)
                CheckJson("Output", OutputJson);
        }

        /// <summary>
        /// Add an error when [json] is filled with something that isn't JSON. An empty value is
        /// valid, it simply means the node holds nothing.
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
            string? input = NullIfEmpty(InputJson);
            string? parameters = NullIfEmpty(ParametersJson);
            string? output = NullIfEmpty(OutputJson);

            if (_control?.IsStart() == true)
            {
                // Every start hands the same input over : they all carry the schema of the workflow
                List<GraphControl> starts = [.. Workflow.Graph.GetStartNodes()];
                string? previousInput = Workflow.InputSchemaJson;
                List<string?> previousOutputs = [.. starts.Select(x => x.OutputSchemaJson)];

                return new ReversibleAction(
                    $"Edit the input of '{Workflow.Metadata.Name}'",
                    () =>
                    {
                        Workflow.InputSchemaJson = input;
                        foreach (GraphControl start in starts)
                            start.OutputSchemaJson = input;
                    },
                    () =>
                    {
                        Workflow.InputSchemaJson = previousInput;
                        foreach ((GraphControl start, string? previous) in starts.Zip(previousOutputs))
                            start.OutputSchemaJson = previous;
                    });
            }

            if (_control?.IsEnd() == true)
            {
                // The mapping of the end is the output of the workflow, each end having its own
                string? previousParameters = Node.ParametersJson;
                string? previousMapping = Workflow.OutputMappingJson;
                string? previousOutput = Workflow.OutputSchemaJson;

                return new ReversibleAction(
                    $"Edit the output of '{Workflow.Metadata.Name}'",
                    () =>
                    {
                        Node.ParametersJson = parameters;
                        Workflow.OutputMappingJson = parameters;
                        Workflow.OutputSchemaJson = output;
                    },
                    () =>
                    {
                        Node.ParametersJson = previousParameters;
                        Workflow.OutputMappingJson = previousMapping;
                        Workflow.OutputSchemaJson = previousOutput;
                    });
            }

            if (_control?.IsShare() == true)
            {
                string? previousParameters = Node.ParametersJson;
                string? previousShared = Workflow.SharedSchemaJson;

                return new ReversibleAction(
                    $"Edit the shared values of '{Node.Name}'",
                    () =>
                    {
                        Node.ParametersJson = parameters;
                        Workflow.SharedSchemaJson = output;
                    },
                    () =>
                    {
                        Node.ParametersJson = previousParameters;
                        Workflow.SharedSchemaJson = previousShared;
                    });
            }

            if (_control?.IsJoin() == true)
            {
                string? previousParameters = Node.ParametersJson;
                string? previousOutput = Node.OutputSchemaJson;

                return new ReversibleAction(
                    $"Edit the merge of '{Node.Name}'",
                    () =>
                    {
                        Node.ParametersJson = parameters;
                        Node.OutputSchemaJson = output;
                    },
                    () =>
                    {
                        Node.ParametersJson = previousParameters;
                        Node.OutputSchemaJson = previousOutput;
                    });
            }

            // A task only owns its parameters, its schemas come from the package it targets
            string? previousSettings = Node.ParametersJson;

            return new ReversibleAction(
                $"Edit the settings of '{Node.Name}'",
                () => Node.ParametersJson = parameters,
                () => Node.ParametersJson = previousSettings);
        }

        /// <summary>
        /// An empty text box means the node holds nothing, which is null rather than "".
        /// </summary>
        private static string? NullIfEmpty(string? json) => string.IsNullOrWhiteSpace(json) ? null : json;

        partial void OnInputJsonChanged(string? value)
        {
            // A start outputs the very input of the workflow, there is nothing else to display
            if (_control?.IsStart() == true)
                OutputJson = value;
            Refresh();
        }

        partial void OnParametersJsonChanged(string? value) => Refresh();

        partial void OnOutputJsonChanged(string? value) => Refresh();
    }
}
