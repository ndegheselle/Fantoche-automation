using Automation.Plugins.Shared;
using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    /// <summary>
    /// Task instance
    /// </summary>
    [BsonKnownTypes(typeof(AutomationSubTaskInstance))]
    [JsonDerivedType(typeof(AutomationSubTaskInstance), "sub")]
    public class AutomationTaskInstance : IAutomationTaskInstance
    {
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>
        /// Task object of the TaskId
        /// </summary>
        [BsonIgnore, JsonIgnore]
        public AutomationTask? Task { get; set; }
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

    /// <summary>
    /// Task instance created from a graph
    /// </summary>
    public class AutomationSubTaskInstance : AutomationTaskInstance
    {
        public Guid WorkflowId { get; set; }
        public Guid GraphNodeId { get; set; }

        public AutomationSubTaskInstance(Guid workflowId, GraphTask node, TaskParameters parameters) : base(node.TaskId, parameters)
        {
            WorkflowId = workflowId;
            GraphNodeId = node.Id;
        }
    }
}
