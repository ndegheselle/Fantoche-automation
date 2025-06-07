using Automation.App.Shared.ViewModels.Work;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskProgressionModal.xaml
    /// </summary>
    public partial class TaskProgressionModal : UserControl, IModalContent
    {
        
        private readonly TasksRealtimeClient _taskRealtimeClient;

        public AutomationTaskInstance Instance { get; private set; }
        public ObservableCollection<TaskProgress> ProgressMessages { get; private set; } = [];

        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Task progression" };

        public TaskProgressionModal(AutomationTaskInstance instance)
        {
            RedisConnectionManager redis = Services.Provider.GetRequiredService<Lazy<RedisConnectionManager>>().Value;

            Instance = instance;
            _taskRealtimeClient = new TasksRealtimeClient(redis.Connection, Instance.Id);
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
            Instance.State = state;
        }

        /// <summary>
        /// Trigger then the instance send progress
        /// </summary>
        /// <param name="progress"></param>
        private void OnInstanceMessage(TaskProgress? progress)
        {
            if (progress != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    ProgressMessages.Add(progress);
                });
            }
        }
    }
}
