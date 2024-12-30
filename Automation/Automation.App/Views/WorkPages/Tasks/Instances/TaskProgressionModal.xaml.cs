using Automation.App.Shared.ViewModels.Work;
using Joufflu.Popups;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskProgressionModal.xaml
    /// </summary>
    public partial class TaskProgressionModal : UserControl, IModalContentValidation
    {
        private readonly TaskInstance _instance;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Task progression" };

        public TaskProgressionModal(TaskInstance instance)
        {
            _instance = instance;
            InitializeComponent();
        }

        public Task<bool> OnValidation()
        {
            // Subscribe to realtime client

            return Task.FromResult(true);
        }
    }
}
