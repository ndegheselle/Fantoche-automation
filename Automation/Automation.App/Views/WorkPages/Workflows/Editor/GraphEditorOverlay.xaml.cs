using Automation.App.ViewModels.Workflow.Editor;
using Joufflu.Popups;
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
        #endregion

        public GraphEditorViewModel Editor
        {
            get { return (GraphEditorViewModel)GetValue(EditorProperty); }
            set { SetValue(EditorProperty, value); }
        }

        private IModal _modal => this.GetCurrentModalContainer();

        public GraphEditorOverlay()
        {
            InitializeComponent();
        }

        #region UI events
        private void OpenHelp_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new GraphEditorHelp());
        }
        #endregion
    }
}