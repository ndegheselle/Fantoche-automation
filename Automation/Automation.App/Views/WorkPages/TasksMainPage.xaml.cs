using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Automation.App.Views.WorkPages.Scopes;
using Automation.App.Views.WorkPages.Tasks;
using Automation.App.Views.WorkPages.Workflows;
using Automation.Shared.Data;
using Joufflu.Shared.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class TasksMainPage : UserControl, ILayout, INotifyPropertyChanged
    {
        public ILayout? ParentLayout { get; set; }
        public Scope? CurrentScope { get; set; }

        private readonly ScopesClient _client;

        public TasksMainPage()
        {
            _client = Services.Provider.GetRequiredService<ScopesClient>();
            InitializeComponent();
            OnLoaded();
        }

        protected async void OnLoaded()
        {
            CurrentScope = await _client.GetRootAsync();
            CurrentScope.Refresh();
            ScopedSelector_SelectedChanged(CurrentScope);
        }

        #region ILayout
        public void Show(IPage page)
        {
            NavigationContent.Content = page;
        }

        public void Hide()
        {
            NavigationContent.Content = null;
        }
        #endregion

        #region UI events
        private void ScopedSelector_SelectedChanged(ScopedElement? selected)
        {
            if (selected == null)
                return;

            switch (selected.Metadata.Type)
            {
                case EnumScopedType.Scope:
                    Show(new ScopePage((Scope)selected));
                    break;
                case EnumScopedType.Workflow:
                    Show(new WorkflowPage((AutomationWorkflow)selected));
                    break;
                case EnumScopedType.Task:
                    Show(new TaskPage((AutomationTask)selected));
                    break;
            }
        }

        private async void ScopedBreadcrumb_ScopeSelected(Scope scope)
        {
            if (CurrentScope == null)
                return;
            CurrentScope = await _client.GetByIdAsync(scope.Id) ?? throw new Exception("Breadcrumb scope cannot be found.");
            CurrentScope.Refresh();
            ScopedSelector_SelectedChanged(CurrentScope);
        }
        #endregion
    }
}
