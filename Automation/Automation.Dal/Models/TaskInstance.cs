using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class TaskInstance : ITaskInstance
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public string WorkerId { get; set; }

        public dynamic Parameters { get; set; }
        public EnumTaskState State { get; set; }
    }
}
