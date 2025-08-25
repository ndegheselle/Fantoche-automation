using Automation.Shared.Data;
using System.Text.Json.Serialization;
using Usuel.Shared.Data;

namespace Automation.Dal.Models
{
    [Flags]
    public enum EnumTaskState
    {
        /// <summary>
        /// Waiting for a worker to execute the task
        /// </summary>
        Pending = 1,
        /// <summary>
        /// Task is waiting on another task, a manual action or a timer
        /// </summary>
        Waiting = 2,
        /// <summary>
        /// Task is progressing
        /// </summary>
        Progressing = 4,
        /// <summary>
        /// Task is completed
        /// </summary>
        Completed = 8,
        /// <summary>
        /// Task failed
        /// </summary>
        Failed = 16,
        /// <summary>
        /// Task is canceled
        /// </summary>
        Canceled = 32,
        /// <summary>
        /// Task is finished
        /// </summary>
        Finished = Completed | Failed | Canceled
    }

    // XXX : may be replaced by a simple guid if there is no need for more information
    public partial class TaskConnector
    {
        public Guid Id { get; set; }
        public SchemaProperty Property { get; set; }
    }

    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public IEnumerable<TaskConnector> Inputs { get; set; } = [new TaskConnector()];
        public IEnumerable<TaskConnector> Outputs { get; set; } = [new TaskConnector()];

        public IEnumerable<Schedule> Schedules { get; set; } = [];

        public BaseAutomationTask(EnumScopedType type) : base(type)
        { }

        public BaseAutomationTask(ScopedMetadata metadata) : base(metadata)
        { }
    }

    public class AutomationTask : BaseAutomationTask
    {
        public TaskTarget? Target { get; set; }
        public AutomationTask() : base(EnumScopedType.Task)
        { }
    }

    public class AutomationControl : AutomationTask
    {
        /// <summary>
        /// Type of the class that the target point on
        /// </summary>
        [JsonIgnore]
        public Type Type { get; set; }
        public AutomationControl(Type type)
        {
            Type = type;
        }
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public Graph Graph { get; set; } = new Graph();
        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {}
    }
}
