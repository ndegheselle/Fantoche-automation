using Automation.Shared.Base;
using Automation.Shared.Data.Task;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Automation.Models.Work
{
    /// <summary>
    /// Task instance
    /// </summary>
    [JsonDerivedType(typeof(SubTaskInstance), "sub")]
    public class TaskInstance : IIdentifier, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Guid Id { get; set; }
        public Guid TaskId { get; set; }

        public string? WorkerId { get; set; }

        public JToken? InputToken { get; set; }
        public JToken? ContextToken { get; set; }
        public JToken? OutputToken { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public TaskInstance(Guid taskId)
        {
            TaskId = taskId;
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Task instance created from a graph
    /// </summary>
    public class SubTaskInstance : TaskInstance
    {
        public Guid WorkflowInstanceId { get; set; }
        public Guid GraphNodeId { get; set; }

        public SubTaskInstance(Guid workflowInstanceId, BaseGraphTask node) : base(node.TaskId)
        {
            WorkflowInstanceId = workflowInstanceId;
            GraphNodeId = node.Id;
        }
    }
}
