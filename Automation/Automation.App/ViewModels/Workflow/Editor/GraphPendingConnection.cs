using Automation.Dal.Models;
using Automation.App.ViewModels.Workflow.Editor.Actions;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphPendingConnection
    {
        private readonly GraphEditorViewModel _editor;
        private GraphConnector? _source;

        public GraphPendingConnection(GraphEditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<GraphConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<GraphConnector>(target =>
            {
                if (target == null || _source == null)
                    return;

                if (_source == target)
                {
                    _editor.RaiseAlert("Can't connect a connector to itself.");
                    return;
                }
                /* TODO : add back type and direction
                if (_source.Type != target.Type)
                {
                    _editor.RaiseAlert("Can't connect two connectors of different types.");
                    return;
                }

                if (_source.Direction == target.Direction)
                {
                    _editor.RaiseAlert("Can't connect two output or input connectors.");
                    return;
                }

                // If target is output, swap source and target
                if (target.Direction == EnumTaskConnectorDirection.Out)
                {
                    var temp = _source;
                    _source = target;
                    target = temp;
                }
                */
                // If already exists delete the connection
                var existingConnection = _editor.Graph.Connections.FirstOrDefault(x => x.Source == _source && x.Target == target);
                if (existingConnection != null)
                {
                    _editor.Actions.Execute(new ConnectionsRemoveAction([existingConnection]));
                    return;
                }

                _editor.Actions.Execute(new ConnectionsAddAction([new GraphConnection(_source, target)]));
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }

}
