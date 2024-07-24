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
        public ScopeItem Scoped { get; set; }
        public Scope? Scope { get; set; }

        private readonly IModalContainer _modal;

        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _scopeClient;
        private readonly ITaskClient _taskClient;

        public ScopePage(IModalContainer modal, ScopeItem scope)
        {
            _scopeClient = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            _modal = modal;

            Scoped = scope;
            InitializeComponent();
            LoadScope(scope);
        }

        public async void LoadScope(ScopeItem scope)
        {
            Scope? fullScope = await _scopeClient.GetScopeAsync(scope.TargetId, new ScopeLoadOptions() { WithChildrens = false});

            if (fullScope == null)
                throw new ArgumentException("Scope not found");
            Scope = fullScope;
        }

        #region UI Events
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            ContextMenu contextMenu = element.ContextMenu;
            contextMenu.PlacementTarget = element;
            contextMenu.IsOpen = true;
        }

        private async void MenuAddScope_Click(object sender, RoutedEventArgs e)
        {
            Scope newScope = new Scope();
            if (await _modal.Show(new ScopeEditModal(newScope)))
            {
                newScope = await _scopeClient.CreateScopeAsync(newScope);
                Scoped.Childrens.Add(new ScopeItem(newScope));
            }
        }

        private async void MenuAddTask_Click(object sender, RoutedEventArgs e)
        {
            var task = new TaskNode();
            if (await _modal.Show(new TaskEditModal(task)))
            {
                task= await _taskClient.CreateTaskAsync(task);
                Scoped.Childrens.Add(new ScopedTaskItem(task));
            }
        }

        private async void MenuAddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            WorkflowNode workflow = new WorkflowNode();
            if (await _modal.Show(new WorkflowEditModal(workflow)))
            {
                workflow = await _taskClient.CreateWorkflowAsync(workflow);
                Scoped.Childrens.Add(new ScopedTaskItem(workflow));
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
