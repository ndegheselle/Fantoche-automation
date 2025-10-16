using Automation.Models.Work;
using Joufflu.Popups;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Workflows.Editor
{
    /// <summary>
    /// Logique d'interaction pour SettingsModal.xaml
    /// </summary>
    public partial class SettingsModal : UserControl, IModalContent
    {
        public IModal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Settings" };

        public GraphTask Task { get; private set; }

        public SettingsModal(GraphTask task)
        {
            Task = task;
            Options.Title = $"Settings - {Task.Name}";
            InitializeComponent();
        }
    }
}
