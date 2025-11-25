using Automation.App.Shared.ApiClients;
using Automation.Models;
using Automation.Models.Work;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour InstanceDetail.xaml
    /// </summary>
    public partial class InstanceDetailModal : UserControl, IModalContent, INotifyPropertyChanged
    {
        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; } = new ModalOptions() { Title = "Instance detail" };
        public TaskInstance Instance { get; private set; }

        public string ContextJson { get; set; } = "";
        public string ResultJson { get; set; } = "";

        private readonly TaskInstancesClient _instanceClient;

        public InstanceDetailModal(TaskInstance instance)
        {
            _instanceClient = Services.Provider.GetRequiredService<TaskInstancesClient>();
            Instance = instance;
            InitializeComponent();
            _ = LoadFullInstance(Instance.Id);
        }

        private async Task LoadFullInstance(Guid instanceId)
        {
            Instance = await _instanceClient.GetByIdAsync(instanceId) ?? throw new ArgumentException("Instance not found");
            ContextJson = JsonSerializer.Serialize("TODO", new JsonSerializerOptions() { WriteIndented = true });
        }
    }
}
