using Automation.App.ViewModels.Graph;
using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
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
        private readonly App _app = (App)App.Current;
        private readonly INodeRepository _repository;

        public WorkflowPage(Guid id)
        {
            _repository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            InitializeComponent();
            LoadWokflow(id);
        }

        public async void LoadWokflow(Guid id)
        {
            // Load full workflow
            if (await _repository.GetNodeAsync(id) is not WorkflowNode workflow)
                throw new ArgumentException("Workflow not found");
            Editor = new EditorViewModel(workflow);
            this.DataContext = this;
        }
    }
}
