using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.WorkPages.Scopes;
using Automation.App.Views.WorkPages.Tasks;
using Automation.App.Views.WorkPages.Workflows;
using Automation.Shared.Data;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class TasksMainPage : UserControl, ILayout
    {
        public ILayout? ParentLayout { get; set; }

        private readonly App _app = App.Current;
        private readonly ScopesClient _client;

        public TasksMainPage()
        {
            _client = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            InitializeComponent();
            OnLoaded();
        }

        protected async void OnLoaded()
        {
            SideMenu.RootScope = await _client.GetRootAsync();
            SideMenu.RootScope.RefreshChildrens();
            SideMenu.Selected = SideMenu.RootScope;
        }

        private void ScopedSelector_SelectedChanged(ScopedElement? selected)
        {

            if (selected == null)
                return;

            switch (selected.Type)
            {
                case EnumScopedType.Scope:
                    Show(new ScopePage((Shared.ViewModels.Work.Scope)selected));
                    break;
                case EnumScopedType.Workflow:
                    Show(new WorkflowPage(selected.Id));
                    break;
                case EnumScopedType.Task:
                    Show(new TaskPage((AutomationTask)selected));
                    break;
            }
        }

        public void Show(IPage page)
        {
            NavigationContent.Content = page;
        }

        public void Hide()
        {
            NavigationContent.Content = null;
        }
    }
}
