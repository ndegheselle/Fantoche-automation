using Automation.App.Base;
using Automation.App.ViewModels.Tasks;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages
{
    /// <summary>
    /// Experimenting a system without a navigation service Change content based on the selected item in the side menu
    /// </summary>
    public partial class TasksMainPage : UserControl, INavigationLayout
    {
        public INavigationLayout? Layout { get; set; }
        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _client;

        public TasksMainPage()
        {
            _client = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            InitializeComponent();
            OnLoaded();
        }

        protected async void OnLoaded()
        {
            SideMenu.RootScope = new ScopeItem(await _client.GetRootScopeAsync());
        }

        private void ScopedSelector_SelectedChanged(ScopedItem? selected)
        {

            if (selected == null)
                return;

            switch (selected.Type)
            {
                case EnumScopedType.Scope:
                    Show(new ScopePage(
                        _app.ServiceProvider.GetRequiredService<IModalContainer>(),
                        (ScopeItem)selected));
                    break;
                case EnumScopedType.Workflow:
                    Show(new WorkflowPage((ScopedTaskItem)selected));
                    break;
                case EnumScopedType.Task:
                    Show(new TaskPage((ScopedTaskItem)selected));
                    break;
            }
        }

        public void Show(IPage page)
        {
            page.Layout = this;
            NavigationContent.Content = page;
        }
    }
}
