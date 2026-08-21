using Automation.Shared.Data.Graph;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A <see cref="GraphConnection"/> as displayed by a Nodify connection, linking the two
    /// connector view models it was resolved to.
    /// </summary>
    public class ConnectionViewModel
    {
        public GraphConnection Model { get; }

        public ConnectorViewModel Source { get; }

        public ConnectorViewModel Target { get; }

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
