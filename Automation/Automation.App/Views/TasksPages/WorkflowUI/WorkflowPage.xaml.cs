using Automation.App.ViewModels.Graph;
using Automation.Shared.ViewModels;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.WorkflowUI
{
    /// <summary>
    /// Logique d'interaction pour WorkflowPage.xaml
    /// </summary>
    public partial class WorkflowPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public EditorViewModel? Editor { get; set; } = null;
        public WorkflowNode Workflow { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;

        public WorkflowPage(ScopedTask scope)
        {
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            InitializeComponent();
            LoadWokflow(scope);
        }

        public async void LoadWokflow(ScopedTask scope)
        {
            // Load full workflow
            if (await _client.GetNodeAsync(scope.TaskId) is not WorkflowNode workflow)
                throw new ArgumentException("Workflow not found");
            Workflow = workflow;
            Workflow.ParentScope = scope;
            Editor = new EditorViewModel(Workflow);
            this.DataContext = Workflow;
        }
    }
}
