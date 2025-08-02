using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Automation.App.Views.WorkPages.Tasks;
using Automation.App.Views.WorkPages.Workflows;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedContextMenu.xaml
    /// </summary>
    public partial class ScopedContextMenu : ContextMenu
    {
        private IModal _modal => this.GetCurrentModal();

        private readonly ScopesClient _scopeClient;
        private readonly TasksClient _taskClient;
        #region Dependency Properties
        // Dependency property ScopedElement Selected
        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
            nameof(Selected),
            typeof(ScopedElement),
            typeof(ScopedContextMenu),
            new PropertyMetadata(null, (o, d) => ((ScopedContextMenu)o).OnSelectedChanged()));
        #endregion

        public ScopedElement? Selected
        {
            get { return (ScopedElement?)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public ICustomCommand RemoveSelectedCommand { get; set; }

        public ICommand AddScopeCommand { get; set; }

        public ICommand AddTaskCommand { get; set; }

        public ICommand AddWorkflowCommand { get; set; }

        public ScopedContextMenu()
        {
            _scopeClient = Services.Provider.GetRequiredService<ScopesClient>();
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();

            RemoveSelectedCommand = new DelegateCommand(
                OnRemoveSelected,
                // Not the root element
                () => Selected?.Parent != null);
            AddScopeCommand = new DelegateCommand(OnAddScope);
            AddTaskCommand = new DelegateCommand(OnAddTask);
            AddWorkflowCommand = new DelegateCommand(OnAddWorkflow);
            InitializeComponent();
        }

        private void OnSelectedChanged() { 
            RemoveSelectedCommand.RaiseCanExecuteChanged(); 
        }

        private async void OnRemoveSelected()
        {
            if (Selected == null)
                return;

            if (AdonisUI.Controls.MessageBox
                    .Show(
                        "Are you sure you want to remove this element ?",
                        "Confirmation",
                        AdonisUI.Controls.MessageBoxButton.YesNo) !=
                AdonisUI.Controls.MessageBoxResult.Yes)
                return;

            switch (Selected.Metadata.Type)
            {
                case EnumScopedType.Workflow:
                case EnumScopedType.Task:
                    await _taskClient.DeleteAsync(Selected.Id);
                    break;
                case EnumScopedType.Scope:
                    await _scopeClient.DeleteAsync(Selected.Id);
                    break;
            }
            Selected.Parent!.Childrens.Remove(Selected);
        }

        private async void OnAddScope()
        {
            if (Selected is not Scope parentScope)
                return;

            Scope newScope = new Scope();
            newScope.ChangeParent(parentScope);
            if (await _modal.Show(new ScopeCreateModal(newScope)))
            {
                parentScope.AddChild((ScopedElement)newScope);
                newScope.FocusOn = EnumScopedTabs.Settings;
            }
        }

        private async void OnAddTask()
        {
            if (Selected is not Scope parentScope)
                return;

            AutomationTask task = new AutomationTask();
            task.ChangeParent(parentScope);
            if (await _modal.Show(new TaskCreateModal(task)))
            {
                parentScope.AddChild(task);
                task.FocusOn = EnumScopedTabs.Settings;
            }
        }

        private async void OnAddWorkflow()
        {
            if (Selected is not Scope parentScope)
                return;

            AutomationWorkflow workflow = new AutomationWorkflow();
            workflow.ChangeParent(parentScope);
            if (await _modal.Show(new WorkflowCreateModal(workflow)))
            {
                parentScope.AddChild(workflow);
                workflow.FocusOn = EnumScopedTabs.Settings;
            }
        }
    }
}
