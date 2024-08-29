using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.App.Views.TasksPages.TaskUI;
using Automation.App.Views.TasksPages.WorkflowUI;
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
        private readonly ScopeClient _client;

        public TasksMainPage()
        {
            _client = _app.ServiceProvider.GetRequiredService<ScopeClient>();
            InitializeComponent();
            OnLoaded();
        }

        protected async void OnLoaded()
        {
            SideMenu.RootScope = await _client.GetRootAsync();
        }

        private void ScopedSelector_SelectedChanged(ScopedElement? selected)
        {

            if (selected == null)
                return;

            switch (selected.Type)
            {
                case EnumScopedType.Scope:
                    Show(new ScopePage(selected.Id));
                    break;
                case EnumScopedType.Workflow:
                    Show(new WorkflowPage(selected.Id));
                    break;
                case EnumScopedType.Task:
                    Show(new TaskPage(selected.Id));
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
