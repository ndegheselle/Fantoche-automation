using Automation.Shared.Data.Graph;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A <see cref="GraphConnection"/> as displayed by a Nodify connection, linking the two
    /// connector view models it was resolved to.
    /// </summary>
    public partial class ConnectionViewModel : ObservableObject
    {
        public GraphConnection Model { get; }

        public ConnectorViewModel Source { get; }

        public ConnectorViewModel Target { get; }

        /// <summary>
        /// What the node this connection leads to cannot handle when it is reached through it : the
        /// branch is what is wrong rather than the node, which holds up when another one reaches it
        /// (see <see cref="Automation.Shared.Data.Execution.GraphPreviewError.DivergenceEdge"/>).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrors), nameof(ErrorsText))]
        private IReadOnlyList<string> _errors = [];

        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// <see cref="Errors"/> as one block of text, which is what a tooltip shows.
        /// </summary>
        public string ErrorsText => string.Join(Environment.NewLine, Errors);

        public ConnectionViewModel(GraphConnection model, ConnectorViewModel source, ConnectorViewModel target)
        {
            Model = model;
            Source = source;
            Target = target;
        }

        public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
            : this(new GraphConnection(source.Model, target.Model), source, target)
        {
        }
    }
}
