using System.Windows;
using Automation.Shared.Data.Graph;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A <see cref="GraphConnector"/> as displayed by a Nodify connector. The anchor is pushed back
    /// by the view, the connections being drawn between the anchors of their connectors.
    /// </summary>
    public partial class ConnectorViewModel : ObservableObject
    {
        public GraphConnector Model { get; }

        public NodeViewModel Node { get; }

        public string Name => Model.Name;

        [ObservableProperty] private Point _anchor;
        [ObservableProperty] private bool _isConnected;

        public ConnectorViewModel(NodeViewModel node, GraphConnector model)
        {
            Node = node;
            Model = model;
            _isConnected = model.IsConnected;
        }

        partial void OnIsConnectedChanged(bool value) => Model.IsConnected = value;
    }
}
