using Automation.App.Shared.ApiClients;
using Automation.Models.Work;
using Automation.Shared.Data.Task;
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
        
        private readonly TaskProgressHubClient _taskProgressHubClient;

        public TaskInstance Instance { get; private set; }
        public ObservableCollection<TaskInstanceNotification> ProgressMessages { get; private set; } = [];

        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Task progression" };

        public TaskProgressionModal(TaskInstance instance)
        {
            _taskProgressHubClient = Services.Provider.GetRequiredService<TaskProgressHubClient>();

            Instance = instance;
            InitializeComponent();

            _taskProgressHubClient.NotificationReceived += OnNotification;
            _taskProgressHubClient.StateReceived += OnInstanceLifecycleChange;
        }

        public void OnHide()
        {
            _taskProgressHubClient.NotificationReceived -= OnNotification;
            _taskProgressHubClient.StateReceived -= OnInstanceLifecycleChange;
        }

        /// <summary>
        /// Trigger then the instance lifcycle change
        /// </summary>
        /// <param name="instanceState"></param>
        private void OnInstanceLifecycleChange(TaskIntanceState instanceState)
        {
            if (instanceState.InstanceId != Instance.Id)
                return;

            Instance.State = instanceState.State;
        }

        /// <summary>
        /// Trigger then the instance send progress
        /// </summary>
        /// <param name="progress"></param>
        private void OnNotification(TaskInstanceNotification progress)
        {
            if (progress.InstanceId != Instance.Id)
                return;

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                ProgressMessages.Add(progress);
            });
        }
    }
}
