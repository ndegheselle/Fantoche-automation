using Automation.App.Shared.ApiClients;
using Automation.App.ViewModels.Workflow.Editor;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour GraphEditorOverlay.xaml
    /// </summary>
    public partial class GraphEditorOverlay : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(
            nameof(Editor),
            typeof(GraphEditorViewModel),
            typeof(GraphEditorOverlay),
            new PropertyMetadata(null));

        public static readonly DependencyProperty UiProperty = DependencyProperty.Register(
            nameof(Ui),
            typeof(GraphEditor),
            typeof(GraphEditorOverlay),
            new PropertyMetadata(null));
        #endregion

        public GraphEditor Ui
        {
            get { return (GraphEditor)GetValue(UiProperty); }
            set { SetValue(UiProperty, value); }
        }

        public GraphEditorViewModel Editor
        {
            get { return (GraphEditorViewModel)GetValue(EditorProperty); }
            set { SetValue(EditorProperty, value); }
        }

        private IModal _modal => this.GetCurrentModal();

        public GraphEditorOverlay()
        {
            InitializeComponent();
        }

        #region UI events
        private void OpenHelp_Click(object sender, RoutedEventArgs e) { _modal.Show(new GraphEditorHelp()); }
        #endregion
    }
}