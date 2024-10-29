using Automation.Plugins.Shared;
using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class TaskInstance : ITaskInstance
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid ScopeId { get; set; }
        public Guid TaskId { get; set; }
        public string WorkerId { get; set; }

        public object? Parameters { get; set; }
        public EnumTaskState State { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
