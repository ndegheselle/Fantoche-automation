using Automation.Shared.Data.Graph;

namespace Automation.App.Features.Scoped.Workflows.Editor.ViewModels;

/// <summary>
/// View model wrapping a <see cref="GraphConnection"/> between two connectors.
/// Nodify draws the connection between <see cref="ViewModels.ConnectorVm.Anchor"/> of its endpoints.
/// </summary>
internal class ConnectionVm
{
    public GraphConnection Model { get; }
    public ViewModels.ConnectorVm Source { get; }
    public ViewModels.ConnectorVm Target { get; }

    public ConnectionVm(GraphConnection model, ViewModels.ConnectorVm source, ViewModels.ConnectorVm target)
    {
        Model = model;
        Source = source;
        Target = target;
    }
}
