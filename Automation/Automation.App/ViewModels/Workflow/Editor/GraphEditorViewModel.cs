using Automation.App.Shared.Base.History;
using Automation.App.Views.WorkPages.Workflows.Editor;
using Automation.Models.Work;
using System.Collections.ObjectModel;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorViewModel
    {
        public event Action<string>? AlertRaised;

        public GraphPendingConnection? PendingConnection { get; }
        public ObservableCollection<GraphNode> SelectedNodes { get; set; } = [];

        public Graph Graph { get; set; }
        public GraphEditor Ui { get; }
        public GraphEditorActions Actions { get; }
        public GraphEditorSettings Settings { get; }

        public HistoryHandler History { get; }

        public GraphEditorViewModel(GraphEditor ui, Graph graph, GraphEditorSettings settings)
        {
            Ui = ui;
            Graph = graph;
            Settings = settings;
            History = new HistoryHandler();
            Actions = new GraphEditorActions(this, History);
            PendingConnection = new GraphPendingConnection(this);
        }

        public void RaiseAlert(string message)
        {
            AlertRaised?.Invoke(message);
        }
    }
}
