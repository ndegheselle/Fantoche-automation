using Automation.App.Base;
using Automation.App.Components.Display;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.TasksPages.Components;
using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowGraph.xaml
    /// </summary>
    public partial class WorkflowGraph : UserControl
    {
        #region Dependency Properties

        // Dependency property Editor of type EditorViewModel
        public static readonly DependencyProperty EditorDataProperty = DependencyProperty.Register(
            "EditorData",
            typeof(EditorViewModel),
            typeof(WorkflowGraph),
            new PropertyMetadata(null, (o, e) => ((WorkflowGraph)o).OnEditorDataChange()));

        public EditorViewModel EditorData
        {
            get => (EditorViewModel)GetValue(EditorDataProperty);
            set => SetValue(EditorDataProperty, value);
        }

        private void OnEditorDataChange() { EditorData.InvalidConnection += _alert.Warning; }
        #endregion

        private readonly App _app = (App)App.Current;
        private readonly IModalContainer _modal;
        private readonly IAlert _alert;
        private readonly INodeRepository _nodeRepository;
        private readonly IScopeRepository _scopeRepository;

        public WorkflowGraph()
        {
            _nodeRepository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            _scopeRepository = _app.ServiceProvider.GetRequiredService<IScopeRepository>();
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            _alert = _app.ServiceProvider.GetRequiredService<IAlert>();
            InitializeComponent();
        }

        #region UI Events
        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            ScopedSelectorModal nodeSelector = new ScopedSelectorModal()
            {
                RootScope = await _scopeRepository.GetRootScopeAsync(),
                AllowedSelectedNodes = EnumScopedType.Workflow | EnumScopedType.Task
            };
            if (await _modal.Show(nodeSelector) && nodeSelector.Selected != null)
            {
                Guid nodeId = ((ScopedNode)nodeSelector.Selected).NodeId;

                if (await _nodeRepository.GetNodeAsync(nodeId) is not WorkflowNode node)
                    throw new ArgumentException("Node not found");
                EditorData.Workflow.Nodes.Add(node);
            }
        }

        // Handle suppr key to remove selected node
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedNodes();
            }
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e) { Editor.ZoomIn(); }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e) { Editor.ZoomOut(); }

        private void ButtonZoomFit_Click(object sender, RoutedEventArgs e) { Editor.FitToScreen(); }

        private void ToggleButtonSnapping_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = (ToggleButton)sender;
            Editor.GridCellSize = toggleButton.IsChecked == true ? EditorViewModel.GRID_DEFAULT_SIZE : 1;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e) { DeleteSelectedNodes(); }

        private void ButtonGroup_Click(object sender, RoutedEventArgs e)
        {
            Rect boundingBox = GetSelectedBoundingBox(10);
            EditorData.CreateGroup(boundingBox);
        }
        #endregion

        private async void DeleteSelectedNodes()
        {
            if (await _modal.Show(
                new ConfirmationModal($"Are you sure you want to delete these {Editor.SelectedItems?.Count} nodes ?")) == false)
                return;

            EditorData.RemoveNodes(EditorData.SelectedNodes);
        }

        private Rect GetSelectedBoundingBox(double padding)
        {
            Point min = new Point(double.MaxValue, double.MaxValue);
            Point max = new Point(double.MinValue, double.MinValue);

            if (Editor.SelectedItems == null || Editor.SelectedItems.Count == 0)
                return Rect.Empty;

            foreach (var node in Editor.SelectedItems)
            {
                var container = Editor.ItemContainerGenerator.ContainerFromItem(node) as ItemContainer;
                if (container == null)
                    continue;

                min.X = Math.Min(min.X, container.Location.X);
                min.Y = Math.Min(min.Y, container.Location.Y);
                max.X = Math.Max(max.X, container.Location.X + container.ActualSize.Width);
                max.Y = Math.Max(max.Y, container.Location.Y + container.ActualSize.Height);
            }

            return new Rect(min.X - padding, min.Y - padding, max.X - min.X + padding * 2, max.Y - min.Y + padding * 2);
        }
    }
}