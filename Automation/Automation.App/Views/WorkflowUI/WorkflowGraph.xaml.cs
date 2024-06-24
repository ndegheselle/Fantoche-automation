using Automation.App.Base;
using Automation.App.Components;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.ScopeUI;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

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
            new PropertyMetadata(null));

        public EditorViewModel EditorData
        {
            get => (EditorViewModel)GetValue(EditorDataProperty);
            set => SetValue(EditorDataProperty, value);
        }

        #endregion

        private readonly App _app = (App)App.Current;
        private readonly IModalContainer _modal;

        public WorkflowGraph()
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            InitializeComponent();
        }

        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NodeSelectorModal nodeSelector = new NodeSelectorModal();
            if (await _modal.Show(nodeSelector, new ModalOptions() { Title = "Add node", ValidButtonText = "Select" }))
            {
                // nodeSelector.Selected;
            }
        }
    }
}
