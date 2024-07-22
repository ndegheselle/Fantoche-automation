using Automation.App.Base;
using Automation.App.ViewModels.Tasks;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.ScopeUI
{
    /// <summary>
    /// Logique d'interaction pour ScopePage.xaml
    /// </summary>
    public partial class ScopePage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        private readonly IModalContainer _modal;
        private readonly ScopeItem _scope;

        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _scopeClient;
        private readonly ITaskClient _taskClient;

        public ScopePage(IModalContainer modal, ScopeItem scope)
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<ITaskClient>();

            _scope = scope;
            _modal = modal;
            this.DataContext = _scope;
            InitializeComponent();
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private void ButtonParam_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new ScopeEditModal(_scope));
        }

        private async void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            ScopeItem newScope = new ScopeItem(new Scope());
            if (await _modal.Show(new ScopeEditModal(newScope)))
            {
                newScope.ScopeNode = await _scopeClient.CreateScopeAsync(newScope.ScopeNode);
                _scope.Childrens.Add(newScope);
            }
        }

        private async void MenuAddTask_Click(object sender, RoutedEventArgs e)
        {
            ScopedTaskItem taskScopedItem = new ScopedTaskItem(new TaskNode());
            if (await _modal.Show(new TaskEditModal(taskScopedItem)))
            {
                taskScopedItem.TaskNode = await _taskClient.CreateTaskAsync(taskScopedItem.TaskNode);
                _scope.Childrens.Add(taskScopedItem);
            }
        }

        private async void MenuAddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            WorkflowScopedItem workflowItem = new WorkflowScopedItem(new WorkflowNode());
            if (await _modal.Show(new WorkflowEditModal(workflowItem)))
            {
                workflowItem.WorkflowNode = await _taskClient.CreateWorkflowAsync(workflowItem.WorkflowNode);
                _scope.Childrens.Add(workflowItem);
            }
        }
        #endregion

        private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListBox listBox = (ListBox)sender;
            ScopedItem? selectedElement = listBox.SelectedItem as ScopedItem;

            if (selectedElement == null)
                return;

            selectedElement.IsSelected = true;
        }
    }
}
