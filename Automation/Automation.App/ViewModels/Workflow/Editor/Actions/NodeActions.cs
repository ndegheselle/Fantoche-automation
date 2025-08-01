using Automation.App.Shared.Base.History;
using Automation.Dal.Models;

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
            RemoveCommand = new ReversibleCommand<GraphNode>(_history, OnRemove);
            RemoveCommand.Reverse = AddCommand;
            AddCommand.Reverse = RemoveCommand;
        }

        public void Add(GraphNode node) => AddCommand.Execute(node);
        private void OnAdd(GraphNode node)
        {
            _editor.Graph.Nodes.Add(node);
        }

        public void Remove(GraphNode node) => RemoveCommand.Execute(node);
        private void OnRemove(GraphNode node)
        {
            _editor.Graph.Nodes.Remove(node);
        }
    }
}
