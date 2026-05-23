using Automation.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Instance of a task that has been executed. Can stand alone (single task) or
    /// be linked to siblings via <see cref="Previous"/>/<see cref="Nexts"/> to form
    /// the execution tree of a workflow.
    /// </summary>
    public class NodeInstance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of the executed task definition.
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Id of the graph node when the instance comes from a workflow, null when standalone.
        /// </summary>
        public Guid? GraphNodeId { get; set; }

        /// <summary>
        /// Name of the node (used as key when building contexts).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public JToken? Input { get; set; }
        public JToken? Output { get; set; }

        public List<NodeInstance> Previous { get; set; } = [];
        public List<NodeInstance> Nexts { get; set; } = [];

        public EnumTaskState State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public NodeInstance()
        {
            CreatedAt = DateTime.UtcNow;
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
