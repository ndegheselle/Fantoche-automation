using Automation.Plugins.Shared;
using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    /// <summary>
    /// Task instance
    /// </summary>
    [BsonKnownTypes(typeof(SubTaskInstance))]
    [JsonDerivedType(typeof(SubTaskInstance), "sub")]
    public class TaskInstance : ITaskInstance
    {
        [BsonId]
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string? WorkerId { get; set; }

        public TaskParameters Parameters { get; set; }
        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public TaskInstance(Guid taskId, TaskParameters parameters)
        {
            TaskId = taskId;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Task instance created from a graph
    /// </summary>
    public class SubTaskInstance : TaskInstance
    {
        public Guid WorkflowId { get; set; }
        public Guid GraphNodeId { get; set; }

        public SubTaskInstance(Guid workflowId, GraphTask node, TaskParameters parameters) : base(node.TaskId, parameters)
        {
            WorkflowId = workflowId;
            GraphNodeId = node.Id;
        }
    }
}
