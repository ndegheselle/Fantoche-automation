using System.Collections.ObjectModel;
using System.Windows;
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

        public NodeViewModel(BaseGraphTask model)
        {
            Model = model;
            _location = new Point(model.LocationX, model.LocationY);

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
