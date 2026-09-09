using System.Collections.ObjectModel;
using System.Windows;
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
        /// only reaches the graph once committed, and it goes through the history of the editor like
        /// any other edition.
        /// </summary>
        [ObservableProperty] private string _nameDraft = string.Empty;

        /// <summary>
        /// State of the last instance of this node in the run being followed, so the editor shows
        /// the progress of the workflow. Null while the node hasn't run yet : the state is that of
        /// a run, not of the graph, and it is cleared when a new one starts.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasState), nameof(StateText))]
        private EnumTaskState? _state;

        /// <summary>
        /// How long the last instance of this node took, null while it hasn't finished : a node
        /// still running has no duration yet, only a start.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private TimeSpan? _duration;

        /// <summary>
        /// What the last instance of this node ended on, for a state that has something to say
        /// (a failure and its message). Null for the rest, the state naming itself.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private string? _stateDetail;

        public bool HasState => State != null;

        /// <summary>
        /// <see cref="State"/> as the reader of the graph gets it : where the node stands, how long
        /// it took and what it ended on. Empty while no run is displayed.
        /// </summary>
        public string StateText
        {
            get
            {
                if (State is not EnumTaskState state)
                    return string.Empty;

                string text = Duration is TimeSpan duration ? $"{state} in {Format(duration)}" : state.ToString();
                return string.IsNullOrEmpty(StateDetail) ? text : $"{text}{Environment.NewLine}{StateDetail}";
            }
        }

        /// <summary>
        /// Display where [state] left the node in the run being followed, [duration] being how long
        /// it took and [detail] what it ended on. Called without anything it clears the run : what
        /// the node shows belongs to one run, and the graph alone says nothing of it.
        /// </summary>
        public void Follow(EnumTaskState? state = null, TimeSpan? duration = null, string? detail = null)
        {
            State = state;
            Duration = duration;
            StateDetail = detail;
        }

        /// <summary>
        /// A duration as the graph shows it : precise on what runs in the blink of an eye, rounded
        /// on what doesn't.
        /// </summary>
        private static string Format(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return $"{duration.TotalMilliseconds:0} ms";
            if (duration.TotalMinutes < 1)
                return $"{duration.TotalSeconds:0.0} s";
            // Counted in hours rather than in days : a task running for a day still reads as the
            // hours it took.
            return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }

        /// <summary>
        /// What the preview of the graph holds against the node : a mapping that cannot resolve what
        /// it reads, one entry per branch it is wrong on. Empty while the node holds up, and read
        /// from the graph rather than from a run, so it shows before anything is started.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrors), nameof(ErrorsText))]
        private IReadOnlyList<string> _errors = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// <see cref="Errors"/> as one block of text, which is what a tooltip shows.
        /// </summary>
        public string ErrorsText => string.Join(Environment.NewLine, Errors);

        public NodeViewModel(BaseGraphTask model)
        {
            Model = model;
            _location = new Point(model.LocationX, model.LocationY);

            // The name is held by the metadata of the node, which the settings of the node edit :
            // the label the editor draws follows it rather than being told to.
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

        /// <summary>
        /// Every connector of the node, inputs first.
        /// </summary>
        public IEnumerable<ConnectorViewModel> Connectors => Inputs.Concat(Outputs);

        partial void OnLocationChanged(Point value)
        {
            Model.LocationX = value.X;
            Model.LocationY = value.Y;
        }
    }
}
