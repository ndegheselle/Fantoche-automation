using Automation.App.Shared.ApiClients;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Tasks.Schedule
{
    /// <summary>
    /// Logique d'interaction pour ScheduleEditModal.xaml
    /// </summary>
    public partial class ScheduleEditModal : UserControl, IModalContent
    {
        
        private readonly TasksClient _taskClient;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Edit schedule" };

        public Automation.Shared.Data.Schedule Schedule { get; set; }
        public ICustomCommand ValidateCommand { get; private set; }

        public ScheduleEditModal(Automation.Shared.Data.Schedule schedule)
        {
            Schedule = schedule;
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
            ValidateCommand = new DelegateCommand(Validate);
            InitializeComponent();
        }

        public void Validate()
        {
            throw new NotImplementedException();
        }
    }
}
