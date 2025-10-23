using Automation.App.Shared.Base.History;
using Automation.App.ViewModels.Workflow.Editor.Actions;
using Automation.App.Views.WorkPages.Workflows.Editor;
using Automation.Models.Work;
using System.Collections.ObjectModel;
using Usuel.History;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorViewModel
    {
        public event Action<string>? AlertRaised;

        public GraphPendingConnection? PendingConnection { get; }
        public ObservableCollection<GraphNode> SelectedNodes { get; set; } = [];

        public Graph Graph { get; set; }
        public GraphEditor Ui { get; }
        public GraphEditorSettings Settings { get; }

        #region Actions
        public HistoryHandler History { get; }

        public NodeActions Nodes { get; private set; }
        public ConnectionsActions Connections { get; private set; }
        #endregion

        public GraphEditorViewModel(GraphEditor ui, Graph graph, GraphEditorSettings settings)
        {
            Ui = ui;
            Graph = graph;
            Settings = settings;
            History = new HistoryHandler();
            PendingConnection = new GraphPendingConnection(this);

            Nodes = new NodeActions(this, History);
            Connections = new ConnectionsActions(this, History);
        }

        public void RaiseAlert(string message)
        {
            AlertRaised?.Invoke(message);
        }
    }
}