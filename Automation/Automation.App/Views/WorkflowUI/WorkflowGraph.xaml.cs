using Automation.App.Base;
using Automation.App.Components;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.ScopeUI;
using Automation.Base;
using Automation.Base.ViewModels;
using Automation.Supervisor.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;

namespace Automation.App.Views.WorkflowUI
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

        private void OnEditorDataChange()
        {
            EditorData.InvalidConnection += _alert.Warning;
        }
        #endregion

        private readonly App _app = (App)App.Current;
        private readonly IModalContainer _modal;
        private readonly IAlert _alert;

        public WorkflowGraph()
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            _alert = _app.ServiceProvider.GetRequiredService<IAlert>();
            InitializeComponent();
        }

        #region UI Events

        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            ScopeRepository scopeRepository = new ScopeRepository();
            ScopedSelectorModal nodeSelector = new ScopedSelectorModal()
            {
                RootScope = scopeRepository.GetRootScope(),
                AllowedSelectedNodes = EnumScopedType.Workflow | EnumScopedType.Task
            };
            if (await _modal.Show(nodeSelector) &&
                nodeSelector.Selected != null)
            {
                Guid nodeId = ((ScopedNode)nodeSelector.Selected).NodeId;
                EditorData.Workflow.Nodes.Add(scopeRepository.GetNode(nodeId));
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

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Editor.ZoomIn();
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Editor.ZoomOut();
        }

        private void ButtonZoomFit_Click(object sender, RoutedEventArgs e)
        {
            Editor.FitToScreen();
        }

        private void ToggleButtonSnapping_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = (ToggleButton)sender;
            Editor.GridCellSize = toggleButton.IsChecked == true ? EditorViewModel.GRID_DEFAULT_SIZE : 1;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedNodes();
        }

        private void ButtonGroup_Click(object sender, RoutedEventArgs e)
        {
            Rect boundingBox = GetSelectedBoundingBox(10);
            EditorData.CreateGroup(boundingBox);
        }


        private async void MenuItemAddInput_Click(object sender, RoutedEventArgs e)
        {
            ConnectorEditModal connectorEditModal = new ConnectorEditModal(new NodeConnector());
            if (await _modal.Show(connectorEditModal))
            {
            }
        }

        private void MenuItemAddOutput_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        private void DeleteSelectedNodes()
        {
            EditorData.RemoveNodes(EditorData.SelectedNodes);
        }

        private Rect GetSelectedBoundingBox(double padding)
        {
            Point min = new Point(double.MaxValue, double.MaxValue);
            Point max = new Point(double.MinValue, double.MinValue);

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