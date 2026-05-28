using System.Text.Json.Serialization;
using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution
{
    /// <summary>
    /// Instance of a task that has been executed. Can stand alone (single task) or
    /// be linked to siblings via <see cref="Previous"/>/<see cref="Nexts"/> to form
    /// the execution tree of a workflow.
    /// </summary>
    public class TaskInstance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Id of the executed task definition.
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// Id of the graph node when the instance comes from a workflow, null when standalone.
        /// </summary>
        public Guid? NodeId { get; set; }
        /// <summary>
        /// Id of the workflow instance that contains this node, null when standalone.
        /// </summary>
        public Guid? ParentInstanceId { get; set; }

        /// <summary>
        /// Name of the node (used as key when building contexts).
        /// </summary>
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// Resolved parameters of the task — i.e. the node's <see cref="Automation.Shared.Data.Graph.BaseGraphTask.ParametersJson"/>
        /// template with context references replaced. This is NOT the data flowing in from
        /// upstream tasks (that lives in the context as <c>previous.*</c>).
        /// </summary>
        public JToken? Parameters { get; set; }
        public JToken? Output { get; set; }

        public List<TaskInstance> Previous { get; set; } = [];
        public List<TaskInstance> Nexts { get; set; } = [];

        private EnumTaskState _state;
        public EnumTaskState State
        {
            get => _state;
            set
            {
                if ((_state & EnumTaskState.Finished) == 0)
                    FinishedAt = DateTime.UtcNow;
                _state = value;
            }
        }

        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        /// <summary>
        /// Graph node currently being executed. Only set while the instance is being driven
        /// by an executor — not persisted.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public BaseGraphTask? Node { get; set; }

        /// <summary>
        /// Parent workflow instance when this task is executed as a node of a workflow.
        /// Not persisted — re-populated by the executor when running.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public WorkflowInstance? ParentWorkflow { get; set; }

        public TaskInstance()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public void Link(TaskInstance previous)
        {
            this.Previous.Add(previous);
            previous.Nexts.Add(this);
        }
    }
}
