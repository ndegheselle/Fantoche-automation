using Automation.App.Base;
using Automation.App.Shared.ViewModels.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    public class TaskEditModal : TaskEdit, IModalContent
    {
        public IModalContainer? ModalParent { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Edit task", ValidButtonText = "Save" };

        public TaskEditModal(TaskNode task) : base(task)
        {
            if (Task.Id == Guid.Empty)
                Options.Title = "New task";
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(nameof(Task), typeof(TaskNode), typeof(TaskEdit), new PropertyMetadata(null));

        public TaskNode Task
        {
            get { return (TaskNode)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public TaskEdit()
        {
            InitializeComponent();
        }

        public TaskEdit(TaskNode task)
        {
            Task = task;
            InitializeComponent();
        }
    }
}
