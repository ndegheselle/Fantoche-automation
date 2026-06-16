using System.Collections.Generic;
using System.Collections.ObjectModel;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Scoped.Workflows.Editor.ViewModels;

/// <summary>
/// View model wrapping a <see cref="BaseGraphTask"/> node displayed on the editor canvas.
/// </summary>
internal partial class NodeVm : ObservableObject
{
    public BaseGraphTask Model { get; }

    public ScopedMetadata Metadata => Model.Metadata;
    public string Name => Model.Name;
    public bool IsReadOnly => Model.Metadata.IsReadOnly;

    public ObservableCollection<ConnectorVm> Inputs { get; }
    public ObservableCollection<ConnectorVm> Outputs { get; }

    /// <summary>
    /// Canvas position, bound two-way to the Nodify item container; persisted back to the model.
    /// </summary>
    [ObservableProperty]
    private Point _location;

    public NodeVm(BaseGraphTask model, IEnumerable<ConnectorVm> inputs, IEnumerable<ConnectorVm> outputs)
    {
        Model = model;
        _location = new Point(model.LocationX, model.LocationY);
        Inputs = new ObservableCollection<ConnectorVm>(inputs);
        Outputs = new ObservableCollection<ConnectorVm>(outputs);
    }

    partial void OnLocationChanged(Point value)
    {
        Model.LocationX = value.X;
        Model.LocationY = value.Y;
    }
}
