using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.ViewModels;
using Automation.App.Views.TasksPages.Components;
using Automation.Shared.Data;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using MessageBox = AdonisUI.Controls.MessageBox;
using Point = System.Drawing.Point;

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
        private IDialogLayout _modal => this.GetCurrentModalContainer();
        private IAlert _alert => this.GetCurrentAlertContainer();
        private readonly TasksClient _nodeClient;
        private readonly ScopesClient _scopeClient;

        public WorkflowGraph()
        {
            _nodeClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            InitializeComponent();
        }

        #region UI Events
        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            ScopedSelectorModal nodeSelector = new ScopedSelectorModal()
            {
                RootScope = await _scopeClient.GetRootAsync(),
                AllowedSelectedNodes = EnumScopedType.Workflow | EnumScopedType.Task
            };

            if (await _modal.ShowDialog(nodeSelector) && nodeSelector.Selected != null)
            {
                TaskNode taskScopedItem = (TaskNode)nodeSelector.Selected;
                EditorData.AddNode(Editor.ViewportLocation, taskScopedItem);
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
            Rectangle boundingBox = GetSelectedBoundingBox(10);
            EditorData.CreateGroup(boundingBox);
        }
        #endregion

        private async void DeleteSelectedNodes()
        {
            if (MessageBox.Show($"Are you sure you want to delete these {Editor.SelectedItems?.Count} nodes ?",
                "Confirmation", AdonisUI.Controls.MessageBoxButton.YesNo) != AdonisUI.Controls.MessageBoxResult.Yes)
                return;

            EditorData.RemoveNodes(EditorData.SelectedNodes);
        }

        private Rectangle GetSelectedBoundingBox(int padding)
        {
            Point min = new Point(int.MaxValue, int.MaxValue);
            Point max = new Point(int.MinValue, int.MinValue);

            if (Editor.SelectedItems == null || Editor.SelectedItems.Count == 0)
                return Rectangle.Empty;

            foreach (var node in Editor.SelectedItems)
            {
                var container = Editor.ItemContainerGenerator.ContainerFromItem(node) as ItemContainer;
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