using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Point = System.Drawing.Point;

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
        
        private readonly GraphsClient _graphsClient;

        public GraphEditor()
        {
            _graphsClient = Services.Provider.GetRequiredService<GraphsClient>();
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
                graph = new Graph();
                await _graphsClient.CreateAsync(graph);
            }

            Editor = new GraphEditorViewModel(this, graph, new GraphEditorSettings());
        }

        private Rectangle GetSelectedBoundingBox(int padding)
        {
            Point min = new Point(int.MaxValue, int.MaxValue);
            Point max = new Point(int.MinValue, int.MinValue);

            if (Editor.SelectedNodes == null || Editor.SelectedNodes.Count == 0)
                return Rectangle.Empty;

            foreach (var node in Editor.SelectedNodes)
            {
                var container = NodifyEditorElement.ItemContainerGenerator.ContainerFromItem(node) as ItemContainer;
                if (container == null)
                    continue;

                min.X = Math.Min(min.X, (int)container.Location.X);
                min.Y = Math.Min(min.Y, (int)container.Location.Y);
                max.X = Math.Max(max.X, (int)container.Location.X + (int)container.ActualSize.Width);
                max.Y = Math.Max(max.Y, (int)container.Location.Y + (int)container.ActualSize.Height);
            }

            return new Rectangle(min.X - padding, min.Y - padding, max.X - min.X + padding * 2, max.Y - min.Y + padding * 2);
        }
    }
}