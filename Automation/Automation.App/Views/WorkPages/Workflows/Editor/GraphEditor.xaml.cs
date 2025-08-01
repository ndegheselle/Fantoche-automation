using Automation.App.ViewModels.Workflow.Editor;
using Automation.Dal.Models;
using Nodify;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Usuel.Shared;
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

        public ICommand ZoomInCommand { get; private set; }
        public ICommand ZoomOutCommand { get; private set; }
        public ICommand ZoomFitCommand { get; private set; }

        public AutomationWorkflow Workflow
        {
            get { return (AutomationWorkflow)GetValue(WorkflowProperty); }
            set { SetValue(WorkflowProperty, value); }
        }

        public GraphEditorViewModel? Editor { get; private set; }
        
        public GraphEditor()
        {
            InitializeComponent();
            ZoomInCommand = new DelegateCommand(NodifyEditorElement.ZoomIn);
            ZoomOutCommand = new DelegateCommand(NodifyEditorElement.ZoomOut);
            ZoomFitCommand = new DelegateCommand(() => NodifyEditorElement.FitToScreen());
        }

        private void OnWorkflowChanged()
        {
            if (Workflow == null)
                return;
            Workflow.Graph.Refresh();
            Editor = new GraphEditorViewModel(Workflow.Graph, new GraphEditorSettings());
        }

        private Rectangle GetSelectedBoundingBox(int padding)
        {
            Point min = new Point(int.MaxValue, int.MaxValue);
            Point max = new Point(int.MinValue, int.MinValue);

            if (Editor!.SelectedNodes == null || Editor.SelectedNodes.Count == 0)
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