using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Automation.Plugins.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Task instance (task that have been executed)
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
    /// Task instance created from a graph (with reference to the specific workflow and graph node)
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

    /// <summary>
    /// Notification of a task instance, used to report the progress of a task execution.
    /// </summary>
    public class TaskInstanceNotification
    {
        public Guid? WorkflowInstanceId { get; set; }
        public Guid InstanceId { get; set; }
        public TaskNotification Notification { get; set; }

        public TaskInstanceNotification(Guid instanceId, TaskNotification data)
        {
            InstanceId = instanceId;
            Notification = data;
        }
    }

    /// <summary>
    /// State of a task instance, used to track the lifecycle of a task.
    /// </summary>
    public class TaskInstanceState
    {
        public Guid? WorkflowInstanceId { get; set; }
        public Guid InstanceId { get; set; }

        public EnumTaskState State { get; set; }

        public TaskInstanceState(Guid instanceId, EnumTaskState state)
        {
            InstanceId = instanceId;
            State = state;
        }
    }
}
