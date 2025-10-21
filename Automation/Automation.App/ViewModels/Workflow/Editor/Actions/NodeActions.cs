using Automation.App.Shared.Base.History;
using Automation.Models.Work;

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

            AddCommand = new ReversibleCommand<GraphNode>(_history, OnAdd);
            RemoveCommand = new ReversibleCommand<IEnumerable<GraphNode>>(_history, OnRemove) { IsBatched = true };
            RemoveCommand.Reverse = AddCommand;
            AddCommand.Reverse = RemoveCommand;
        }

        public void Add(GraphNode node) => AddCommand.Execute(node);
        private void OnAdd(GraphNode node)
        {
            _editor.Graph.Nodes.Add(node);
        }

        public void Remove(IEnumerable<GraphNode> nodes) => RemoveCommand.Execute(nodes);
        private void OnRemove(IEnumerable<GraphNode> nodes)
        {
            foreach(GraphNode node in nodes.ToList())
            {
                if (node is GraphTask task)
                    _editor.Actions.Connections.Disconnect(task);
                _editor.Graph.Nodes.Remove(node);
            }
        }
    }
}
