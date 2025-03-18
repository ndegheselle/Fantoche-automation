using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.ViewModels.Workflow.Editor;
using Automation.App.Views.WorkPages.Scopes.Components;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Xaml;

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
        private readonly ScopesClient _client;

        public GraphEditorOverlay()
        {
            _client = Services.Provider.GetRequiredService<ScopesClient>();
            InitializeComponent();
        }

        #region UI events
        private void OpenHelp_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new GraphEditorHelp());
        }

        private async void OpenAddNode_Click(object sender, RoutedEventArgs e)
        {
            var selector = new ScopedSelectorModal();
            if (await _modal.Show(selector) && selector.Selected is AutomationTask task)
            {
                Editor.Commands.AddNode.Execute(new GraphTask(IXamlIndexingReader co));
            }
        }
        #endregion
    }
}