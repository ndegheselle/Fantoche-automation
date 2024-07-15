using Automation.App.ViewModels.Graph;
using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

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
        private readonly INodeRepository _repository;

        public WorkflowPage(ScopedTask scope)
        {
            _repository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            InitializeComponent();
            LoadWokflow(scope);
        }

        public async void LoadWokflow(ScopedTask scope)
        {
            // Load full workflow
            if (await _repository.GetNodeAsync(scope.TaskId) is not WorkflowNode workflow)
                throw new ArgumentException("Workflow not found");
            Workflow = workflow;
            Workflow.ParentScope = scope;
            Editor = new EditorViewModel(Workflow);
            this.DataContext = Workflow;
        }
    }
}
