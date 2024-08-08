using Automation.Shared.Contracts;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class TaskHistory : ITaskHistory
    {
        [BsonId]
        public Guid Id { get; set; }
        [BsonId]
        public Guid? ParentTaskId { get; set; }
        [BsonId]
        public Guid ScopeId { get; set; }
        [BsonId]
        public Guid TaskId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EnumInstanceStatus Status { get; set; }
    }
}
