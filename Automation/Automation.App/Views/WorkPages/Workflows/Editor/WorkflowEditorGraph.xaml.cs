using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor;
using Automation.App.Views.WorkPages.Scopes.Components;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using MessageBox = AdonisUI.Controls.MessageBox;
using Point = System.Drawing.Point;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour WorkflowGraph.xaml
    /// </summary>
    public partial class WorkflowEditorGraph : UserControl
    {
        #region Dependency Properties

        // Dependency property Editor of type EditorViewModel
        public static readonly DependencyProperty EditorDataProperty = DependencyProperty.Register(
            "EditorData",
            typeof(EditorViewModel),
            typeof(WorkflowEditorGraph),
            new PropertyMetadata(null, (o, e) => ((WorkflowEditorGraph)o).OnEditorDataChange()));

        public EditorViewModel EditorData
        {
            get => (EditorViewModel)GetValue(EditorDataProperty);
            set => SetValue(EditorDataProperty, value);
        }

        private void OnEditorDataChange() { EditorData.InvalidConnection += _alert.Warning; }
        #endregion

        private readonly App _app = (App)App.Current;
        private IModal _modal => this.GetCurrentModalContainer();
        private IAlert _alert => this.GetCurrentAlertContainer();
        private readonly TasksClient _nodeClient;
        private readonly ScopesClient _scopeClient;

        public WorkflowEditorGraph()
        {
            _nodeClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _scopeClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            InitializeComponent();
        }

        private void DeleteSelectedNodes()
        {
            if (MessageBox.Show($"Are you sure you want to delete these {EditorData.SelectedItems?.Count} nodes ?",
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