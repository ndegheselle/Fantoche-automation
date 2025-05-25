using Automation.Plugins.Shared;
using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class AutomationTaskInstance : IAutomationTaskInstance
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public TaskParameters Parameters { get; set; }

        public string? WorkerId { get; set; }
        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public AutomationTaskInstance(Guid taskId, TaskParameters parameters)
        {
            TaskId = taskId;
            Parameters = parameters;
        }
    }

    public class AutomationWorflowInstance : AutomationTaskInstance
    {
        // TODO : add instance graph history

        public AutomationWorflowInstance(Guid taskId, TaskParameters parameters) : base(taskId, parameters)
        {
        }
    }
}
