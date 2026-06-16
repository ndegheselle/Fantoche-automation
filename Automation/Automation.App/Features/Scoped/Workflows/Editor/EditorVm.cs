using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.App.Features.Scoped.Workflows.Editor.ViewModels;
using Automation.App.Services;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Scoped.Workflows.Editor;

/// <summary>
/// View model backing the Nodify workflow editor. Wraps the workflow's <see cref="TasksGraph"/> in
/// observable node/connection view models, mediates connection editing, and persists the graph.
/// </summary>
internal partial class EditorVm : ObservableObject
{
    private readonly AutomationWorkflow _workflow;
    private readonly TasksGraph _graph;
    private readonly IScopedService _scopedService;

    // Connector view models shared between nodes and connections, keyed by the model connector id.
    private readonly Dictionary<Guid, ViewModels.ConnectorVm> _connectors = new();

    public ObservableCollection<ViewModels.NodeVm> Nodes { get; } = [];
    public ObservableCollection<ConnectionVm> Connections { get; } = [];

    /// <summary>Bound to <c>NodifyEditor.SelectedItems</c>; holds the selected <see cref="ViewModels.NodeVm"/>.</summary>
    public ObservableCollection<object> SelectedNodes { get; } = [];

    public PendingConnectionVm PendingConnection { get; }

    // Raised for viewport actions that must run against the NodifyEditor control (handled in code-behind).
    public event Action? ZoomInRequested;
    public event Action? ZoomOutRequested;
    public event Action? FitToScreenRequested;

    public EditorVm(AutomationWorkflow workflow)
    {
        _workflow = workflow;
        _graph = workflow.Graph;
        _scopedService = ServiceProvider.Scoped;
        PendingConnection = new PendingConnectionVm(this);

        Load();
    }

    private void Load()
    {
        _graph.Refresh();

        foreach (BaseGraphTask task in _graph.Nodes.OfType<BaseGraphTask>())
        {
            var inputs = task.Inputs.Select(GetOrCreateConnector).ToList();
            var outputs = task.Outputs.Select(GetOrCreateConnector).ToList();
            Nodes.Add(new ViewModels.NodeVm(task, inputs, outputs));
        }

        foreach (GraphConnection connection in _graph.Connections)
        {
            if (_connectors.TryGetValue(connection.SourceId, out ViewModels.ConnectorVm? source) &&
                _connectors.TryGetValue(connection.TargetId, out ViewModels.ConnectorVm? target))
            {
                Connections.Add(new ConnectionVm(connection, source, target));
            }
        }
    }

    private ViewModels.ConnectorVm GetOrCreateConnector(GraphConnector model)
    {
        if (!_connectors.TryGetValue(model.Id, out ViewModels.ConnectorVm? vm))
        {
            vm = new ViewModels.ConnectorVm(model);
            _connectors[model.Id] = vm;
        }

        return vm;
    }

    /// <summary>
    /// Create a connection between two connectors, or remove it if the same connection already exists.
    /// </summary>
    public void ToggleConnection(ViewModels.ConnectorVm source, ViewModels.ConnectorVm target)
    {
        ConnectionVm? existing = Connections.FirstOrDefault(c => c.Source == source && c.Target == target);
        if (existing != null)
        {
            RemoveConnection(existing);
            return;
        }

        var model = new GraphConnection(source.Model, target.Model);
        _graph.Connections.Add(model);
        Connections.Add(new ConnectionVm(model, source, target));
        source.IsConnected = true;
        target.IsConnected = true;
    }

    private void RemoveConnection(ConnectionVm connection)
    {
        _graph.Connections.Remove(connection.Model);
        Connections.Remove(connection);

        // A connector stays "connected" only while at least one connection still references it.
        connection.Source.IsConnected = _graph.GetConnectionsFrom(connection.Source.Model).Any();
        connection.Target.IsConnected = _graph.GetConnectionsFrom(connection.Target.Model).Any();
    }

    private void RemoveNode(ViewModels.NodeVm node)
    {
        var connectorIds = node.Inputs.Concat(node.Outputs).Select(c => c.Model.Id).ToHashSet();
        foreach (ConnectionVm connection in Connections
                     .Where(c => connectorIds.Contains(c.Source.Model.Id) || connectorIds.Contains(c.Target.Model.Id))
                     .ToList())
        {
            RemoveConnection(connection);
        }

        _graph.Nodes.Remove(node.Model);
        Nodes.Remove(node);
    }

    [RelayCommand]
    private void ZoomIn() => ZoomInRequested?.Invoke();

    [RelayCommand]
    private void ZoomOut() => ZoomOutRequested?.Invoke();

    [RelayCommand]
    private void FitToScreen() => FitToScreenRequested?.Invoke();

    [RelayCommand]
    private void RemoveSelection()
    {
        foreach (ViewModels.NodeVm node in SelectedNodes.OfType<ViewModels.NodeVm>().Where(n => !n.IsReadOnly).ToList())
            RemoveNode(node);

        SelectedNodes.Clear();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await _scopedService.EditAsync(_workflow);
            ServiceProvider.Toasts.Value.Success("Workflow saved", $"\"{_workflow.Metadata.Name}\" has been saved.");
        }
        catch (Exception ex)
        {
            ServiceProvider.Toasts.Value.Error("Could not save the workflow", ex.Message);
        }
    }
}
