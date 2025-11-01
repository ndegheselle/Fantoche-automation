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

        internal ReversibleCommand<NodeModifyContext> AddAllCommand { get; private set; }
        internal ReversibleCommand<NodeModifyContext> RemoveAllCommand { get; private set; }

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
            if (task == null)
                return;

            // If the task is the start control task
            if (task is GraphControl control && control.TaskId == AutomationControl.StartTaskId)
            {
                _editor.Ui.Modal.Show(new WorkflowSchemaModal(_editor.Ui.Workflow));
            }
            else
            {
                _editor.Ui.Modal.Show(new TaskInputSettingModal(task));
            }
        }

        public void Add(GraphNode node) { AddAllCommand.Execute(new NodeModifyContext() { Nodes = [node] }); }

        public void Remove(IEnumerable<GraphNode> nodes)
        {
            RemoveAllCommand.Execute(
                new NodeModifyContext()
                {
                    Nodes = nodes,
                    Connections = nodes.OfType<GraphTask>().SelectMany(_editor.Graph.GetConnectionsFrom)
                });
        }

        void AddAll(NodeModifyContext context)
        {
            foreach (GraphNode node in context.Nodes)
                _editor.Graph.Nodes.Add(node);
            _editor.Connections.Connect(context.Connections);
        }

        void RemoveAll(NodeModifyContext context)
        {
            _editor.Connections.Disconnect(context.Connections);
            foreach (GraphNode node in context.Nodes)
                _editor.Graph.Nodes.Remove(node);
        }
    }
}
