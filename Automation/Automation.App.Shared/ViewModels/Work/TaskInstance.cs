using Automation.Plugins.Shared;
using Automation.Shared.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.Shared.ViewModels.Work
{
    public class TaskInstance : ITaskInstance, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }

        public Guid TaskId { get; set; }

        public TargetedPackage Target { get; set; } = new TargetedPackage(new PackageIdentifier(), new PackageClass());
        public string? WorkerId { get; set; }

        public string SettingsJson { get; set; } = "";
        public string ContextJson { get; set; } = "";

        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
