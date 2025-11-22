using Automation.App.Views.WorkPages.Workflows.Editor.Components;
using Automation.Models.Work;
using Usuel.History;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    internal class NodeModifyContext
    {
        public IEnumerable<GraphNode> Nodes { get; set; } = [];

        public IEnumerable<GraphConnection> Connections { get; set; } = [];
    }

    public class NodeActions
    {
        public ICustomCommand AddCommand { get; private set; }
        public ICustomCommand RemoveCommand { get; private set; }
        public ICustomCommand OpenCommand { get; private set; }

        private ReversibleCommand<NodeModifyContext> AddAllCommand { get; }
        private ReversibleCommand<NodeModifyContext> RemoveAllCommand { get; }

        private readonly GraphEditorViewModel _editor;
        private readonly HistoryHandler _history;

        public NodeActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;

            OpenCommand = new DelegateCommand<BaseGraphTask>(Open);
            AddCommand = new DelegateCommand<GraphNode>(Add);
            RemoveCommand = new DelegateCommand<IEnumerable<GraphNode>>(Remove);

            AddAllCommand = new ReversibleCommand<NodeModifyContext>(_history, AddAll);
            RemoveAllCommand = new ReversibleCommand<NodeModifyContext>(_history, RemoveAll);
            _history.SetReverse(AddAllCommand, RemoveAllCommand);
        }

        public void Open(BaseGraphTask task)
        {
            switch (task)
            {
                case null:
                    return;
                // If the task is the start control task or an end task
                case GraphControl control when control.TaskId == AutomationControl.StartTaskId:
                    _editor.Ui.Modal.Show(new WorkflowInputModal(_editor.Ui.Workflow));
                    break;
                case GraphControl control when control.TaskId == AutomationControl.EndTaskId:
                    _editor.Ui.Modal.Show(new WorkflowOutputModal(control, _editor.Ui.Workflow));
                    break;
                default:
                    _editor.Ui.Modal.Show(new TaskInputSettingModal(task, _editor.Ui.Workflow.Graph));
                    break;
            }
        }

        public void Add(GraphNode node)
        {
            if (node is BaseGraphTask task)
                task.Metadata.Name = _editor.Graph.GetUniqueNodeName(task.Metadata.Name);
            AddAllCommand.Execute(new NodeModifyContext() { Nodes = [node] });
        }

        public void Remove(IEnumerable<GraphNode> nodes)
        {
            RemoveAllCommand.Execute(
                new NodeModifyContext()
                {
                    Nodes = nodes,
                    Connections = nodes.OfType<GraphTask>().SelectMany(_editor.Graph.GetConnectionsFrom)
                });
        }

        private void AddAll(NodeModifyContext context)
        {
            foreach (var node in context.Nodes)
                _editor.Graph.Nodes.Add(node);
            _editor.Connections.Connect(context.Connections);
        }

        private void RemoveAll(NodeModifyContext context)
        {
            _editor.Connections.Disconnect(context.Connections);
            foreach (var node in context.Nodes.ToList())
                _editor.Graph.Nodes.Remove(node);
        }
    }
}
