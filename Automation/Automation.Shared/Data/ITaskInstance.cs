using Automation.Plugins.Shared;

namespace Automation.Shared.Data
{
    public interface ITaskInstance : IIdentifier
    {
        // XXX : come from the Task, there to simplify request, what about updates (change task scope)
        public Guid ScopeId { get; set; }
        public Guid TaskId { get; set; }
        public string WorkerId { get; set; }

        public object? Parameters { get; set; }
        public EnumTaskState State { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
