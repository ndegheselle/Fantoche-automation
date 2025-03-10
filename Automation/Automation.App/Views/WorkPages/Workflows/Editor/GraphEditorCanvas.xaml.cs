using Automation.App.Shared.ApiClients;
using Automation.App.ViewModels.Workflow.Editor;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

using MessageBox = AdonisUI.Controls.MessageBox;
using Point = System.Drawing.Point;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour WorkflowGraph.xaml
    /// </summary>
    public partial class GraphEditorCanvas : UserControl
    {
        #region Dependency Properties

        // Dependency property Editor of type EditorViewModel
        public static readonly DependencyProperty EditorDataProperty = DependencyProperty.Register(
            "EditorData",
            typeof(GraphEditorViewModel),
            typeof(GraphEditorCanvas),
            new PropertyMetadata(null));

        public GraphEditorViewModel Editor
        {
            get => (GraphEditorViewModel)GetValue(EditorDataProperty);
            set => SetValue(EditorDataProperty, value);
        }

        #endregion

        
        private IAlert _alert => this.GetCurrentAlertContainer();

        public GraphEditorCanvas()
        {
            InitializeComponent();
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