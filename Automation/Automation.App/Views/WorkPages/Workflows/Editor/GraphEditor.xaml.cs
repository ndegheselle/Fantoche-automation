using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour GraphEditor.xaml
    /// </summary>
    public partial class GraphEditor : UserControl, INotifyPropertyChanged
    {
        #region Dependency properties
        public static readonly DependencyProperty WorkflowProperty = DependencyProperty.Register(
            nameof(Workflow),
            typeof(AutomationWorkflow),
            typeof(GraphEditor),
            new PropertyMetadata(null, (o, d) => ((GraphEditor)o).OnWorkflowChanged()));
        #endregion

        public AutomationWorkflow Workflow
        {
            get { return (AutomationWorkflow)GetValue(WorkflowProperty); }
            set { SetValue(WorkflowProperty, value); }
        }

        public GraphEditorViewModel? Editor { get; private set; }
        private readonly App _app = (App)App.Current;
        private readonly GraphsClient _graphsClient;

        public GraphEditor()
        {
            _graphsClient = _app.ServiceProvider.GetRequiredService<GraphsClient>();
            InitializeComponent(); 
        }

        private async void OnWorkflowChanged()
        {
            if (Workflow == null)
                return;

            // Load graph
            Graph? graph = await _graphsClient.GetByWorkflowIdAsync(Workflow.Id);
            if (graph == null)
            {
                // Create graph associed with workflow if doesn't exist
                graph = new Graph() { WorkflowId = Workflow.Id };
                await _graphsClient.CreateAsync(graph);
            }

            Editor = new GraphEditorViewModel(graph, Canvas, new GraphEditorSettings());
        }
    }
}
