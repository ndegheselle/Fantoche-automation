using Automation.App.Shared.Base.History;
using Automation.App.ViewModels.Workflow.Editor.Actions;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorActions
    {
        public NodeActions Nodes { get; private set; }
        public ConnectionsActions Connections { get; private set; }

        private readonly HistoryHandler _history;
        private readonly GraphEditorViewModel _editor;
        public GraphEditorActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;
            Nodes = new NodeActions(_editor, _history);
            Connections = new ConnectionsActions(_editor, _history);
        }
    }
}
