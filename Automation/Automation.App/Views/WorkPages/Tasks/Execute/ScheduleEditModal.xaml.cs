using Automation.App.Shared.ApiClients;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Execute
{
    /// <summary>
    /// Logique d'interaction pour ScheduleEditModal.xaml
    /// </summary>
    public partial class ScheduleEditModal : UserControl, IModalContentValidation
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Edit schedule" };

        public Schedule Schedule { get; set; }

        public ScheduleEditModal(Schedule schedule)
        {
            Schedule = schedule;
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            InitializeComponent();
        }

        public Task<bool> OnValidation()
        {
            throw new NotImplementedException();
        }
    }
}
