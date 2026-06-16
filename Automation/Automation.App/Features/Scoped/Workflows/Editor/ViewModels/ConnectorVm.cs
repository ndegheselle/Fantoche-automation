using Automation.Shared.Data.Graph;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Scoped.Workflows.Editor.ViewModels;

/// <summary>
/// View model wrapping a <see cref="GraphConnector"/> (an input or output endpoint of a node).
/// </summary>
internal partial class ConnectorVm : ObservableObject
{
    public GraphConnector Model { get; }

    public string Name => Model.Name;

    /// <summary>
    /// Canvas position of the connector, written by Nodify (OneWayToSource) and read by
    /// connections to draw their endpoints.
    /// </summary>
    [ObservableProperty]
    private Point _anchor;

    [ObservableProperty]
    private bool _isConnected;

    public ConnectorVm(GraphConnector model)
    {
        Model = model;
        _isConnected = model.IsConnected;
    }

    partial void OnIsConnectedChanged(bool value) => Model.IsConnected = value;
}
