using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskProgressionModal.xaml
    /// </summary>
    public partial class TaskProgressionModal : UserControl, IModalContent
    {
        private readonly App _app = (App)App.Current;
        private readonly TaskInstance _instance;
        private readonly TasksRealtimeClient _taskRealtimeClient;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Task progression" };

        public TaskProgressionModal(TaskInstance instance)
        {
            RedisConnectionManager redis = _app.ServiceProvider.GetRequiredService<Lazy<RedisConnectionManager>>().Value;

            _instance = instance;
            _taskRealtimeClient = new TasksRealtimeClient(redis.Connection, _instance.Id);
            InitializeComponent();

            _taskRealtimeClient.Progress.Subscribe(OnInstanceMessage);
            _taskRealtimeClient.Lifecycle.Subscribe(OnInstanceLifecycleChange);
        }

        public void OnHide()
        {
            _taskRealtimeClient.Progress.Unsubscribe();
        }

        /// <summary>
        /// Trigger then the instance lifcycle change
        /// </summary>
        /// <param name="state"></param>
        private void OnInstanceLifecycleChange(EnumTaskState state)
        {
        }

        /// <summary>
        /// Trigger then the instance send progress
        /// </summary>
        /// <param name="progress"></param>
        private void OnInstanceMessage(TaskProgress? progress)
        {
        }
    }
}
