using Automation.App.Base;
using Automation.App.ViewModels.Graph;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public TaskNode Task { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly INodeRepository _repository;

        public TaskPage(ScopedTask scope)
        {
            _repository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            InitializeComponent();
            LoadTask(scope);
        }

        public async void LoadTask(ScopedTask scope)
        {
            // Load full workflow
            if (await _repository.GetNodeAsync(scope.TaskId) is not TaskNode task)
                throw new ArgumentException("Task not found.");
            Task = task;
            Task.ParentScope = scope;
            this.DataContext = Task;
        }
    }
}
