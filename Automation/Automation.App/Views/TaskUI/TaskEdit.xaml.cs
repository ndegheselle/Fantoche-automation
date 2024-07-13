using Automation.App.Base;
using Automation.Shared;
using Automation.Shared.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TaskUI
{
    public class TaskEditModal : TaskEdit, IModalContentCallback
    {
        public IModalContainer? ModalParent { get; set; } = null;
        public ModalOptions Options => new ModalOptions() { Title = "Edit task", ValidButtonText = "Save" };

        public TaskEditModal(TaskNode task) : base(task)
        {
            if (task.Id == Guid.Empty)
                Options.Title = "New task";
        }

        public void OnModalClose(bool result)
        {
            // New scope
            if (_task.Id == Guid.Empty)
                _task.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl
    {
        protected readonly TaskNode _task;

        public TaskEdit(TaskNode task)
        {
            _task = task;
            this.DataContext = _task;
            InitializeComponent();
        }
    }
}
