using System.Collections.ObjectModel;
using System.Windows;
using Automation.App.Common;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A <see cref="BaseGraphTask"/> as displayed by a Nodify node : the graph only stores the
    /// location as two doubles, the editor needs a point and the selection state.
    /// </summary>
    public partial class NodeViewModel : ObservableObject
    {
        public BaseGraphTask Model { get; }

        public ScopedMetadata Metadata => Model.Metadata;

        public string Name => Model.Metadata.Name;

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = [];

        public ObservableCollection<ConnectorViewModel> Outputs { get; } = [];

        [ObservableProperty] private Point _location;
        [ObservableProperty] private bool _isSelected;

        /// <summary>
        /// Whether the name is being edited on the graph itself, the label of the node standing in
        /// for the box while it is.
        /// </summary>
        [ObservableProperty] private bool _isRenaming;

        /// <summary>
        /// The name being typed while <see cref="IsRenaming"/>. Held apart from the node : renaming
        /// only reaches the graph once committed, through the history like any other edition.
        /// </summary>
        [ObservableProperty] private string _nameDraft = string.Empty;

        /// <summary>
        /// State of the last instance of this node in the run being followed. Null while the node
        /// hasn't run : the state belongs to a run, not to the graph, and a new run clears it.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasState), nameof(StateText))]
        private EnumTaskState? _state;

        /// <summary>
        /// How long the last instance took, null while it hasn't finished.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private TimeSpan? _duration;

        /// <summary>
        /// What the last instance ended on, for a state that has something to say (a failure and
        /// its message). Null for the rest, the state naming itself.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private string? _stateDetail;

        public bool HasState => State != null;

        /// <summary>
        /// <see cref="State"/> as the graph shows it : where the node stands, how long it took and
        /// what it ended on. Empty while no run is displayed.
        /// </summary>
        public string StateText
        {
            get
            {
                if (State is not EnumTaskState state)
                    return string.Empty;

                string text = Duration is TimeSpan duration ? $"{state} in {Durations.Format(duration)}" : state.ToString();
                return string.IsNullOrEmpty(StateDetail) ? text : $"{text}{Environment.NewLine}{StateDetail}";
            }
        }

        /// <summary>
        /// Display where [state] left the node in the run being followed. Called without anything it
        /// clears the run : what the node shows belongs to one run.
        /// </summary>
        public void Follow(EnumTaskState? state = null, TimeSpan? duration = null, string? detail = null)
        {
            State = state;
            Duration = duration;
            StateDetail = detail;
        }

        /// <summary>
        /// What the preview of the graph holds against the node : a mapping that cannot resolve what
        /// it reads, one entry per branch it is wrong on. Read from the graph rather than from a
        /// run, so it shows before anything is started.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrors), nameof(ErrorsText))]
        private IReadOnlyList<string> _errors = [];

        public bool HasErrors => Errors.Count > 0;

        public string ErrorsText => string.Join(Environment.NewLine, Errors);

        public NodeViewModel(BaseGraphTask model)
        {
            Model = model;
            _location = new Point(model.LocationX, model.LocationY);

            // The name is held by the metadata, which the node settings edit : the label the
            // editor draws follows it rather than being told to.
            Model.Metadata.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScopedMetadata.Name))
                    OnPropertyChanged(nameof(Name));
            };

            foreach (GraphConnector input in model.Inputs)
                Inputs.Add(new ConnectorViewModel(this, input, isOutput: false));
            foreach (GraphConnector output in model.Outputs)
                Outputs.Add(new ConnectorViewModel(this, output, isOutput: true));
        }

        public IEnumerable<ConnectorViewModel> Connectors => Inputs.Concat(Outputs);

        partial void OnLocationChanged(Point value)
        {
            Model.LocationX = value.X;
            Model.LocationY = value.Y;
        }
    }
}
