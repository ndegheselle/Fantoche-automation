using Automation.Models.Work;
using Usuel.History;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class NodeActions
    {
        public IReversibleCommand AddCommand { get; private set; }
        public IReversibleCommand RemoveCommand { get; private set; }

        private readonly GraphEditorViewModel _editor;
        private readonly HistoryHandler _history;
        public NodeActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;

            AddCommand = new ReversibleCommand<GraphNode>(_history, Add);
            RemoveCommand = new ReversibleCommand<IEnumerable<GraphNode>>(_history, Remove);
            RemoveCommand.Reverse = AddCommand;
            AddCommand.Reverse = RemoveCommand;
        }

        public void Add(GraphNode node)
        {
            _editor.Graph.Nodes.Add(node);
        }

        public void Remove(IEnumerable<GraphNode> nodes)
        {
            foreach (GraphNode node in nodes.ToList())
            {
                if (node is GraphTask task)
                    _editor.Actions.Connections.Disconnect(task);
                _editor.Graph.Nodes.Remove(node);
            }
        }
    }
}
