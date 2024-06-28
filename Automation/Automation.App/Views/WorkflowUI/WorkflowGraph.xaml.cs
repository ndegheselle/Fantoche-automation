using Automation.App.Base;
using Automation.App.Components;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.ScopeUI;
using Automation.Base;
using Automation.Supervisor.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Nodify;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

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

            foreach (var test in EditorData.Workflow.Tasks)
            {
                foreach (var input in test.Inputs)
                {
                    input.Anchor = new Point(10, 20);
                }
            }
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

        private async void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            ScopeRepository scopeRepository = new ScopeRepository();
            NodeSelectorModal nodeSelector = new NodeSelectorModal()
            {
                RootScope = scopeRepository.GetRootScope(),
                AllowedSelectedNodes = EnumNodeType.Workflow | EnumNodeType.Task
            };
            if (await _modal.Show(nodeSelector, new ModalOptions() { Title = "Add node", ValidButtonText = "Add" }) &&
                nodeSelector.Selected != null)
            {
                EditorData.Workflow.Tasks.Add((TaskNode)nodeSelector.Selected);
            }
        }

        // Handle suppr key to remove selected node
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var selectedItems = ((MultiSelector)Editor).SelectedItems;
                // Reverse loop for deletion
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    TaskNode? node = selectedItems[i] as TaskNode;
                    if (node == null)
                        return;

                    EditorData.RemoveNode(node);
                }
            }
        }
    }
}
