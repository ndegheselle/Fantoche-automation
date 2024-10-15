using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class TaskInstance : IIdentifier
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public dynamic Parameters { get; set; }
    }
}
