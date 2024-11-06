using Automation.Plugins.Shared;
using Automation.Shared.Data;
using System.ComponentModel;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskInstance : ITaskInstance, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public TargetedPackage Target { get; set; }
        public string? WorkerId { get; set; }

        public TaskContext Context { get; set; }
        public Dictionary<string, object>? Results { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
