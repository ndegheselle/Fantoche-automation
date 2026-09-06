using System.Text.Json.Serialization;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
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

        public JToken? PreviousContext ?
        /// <summary>
        /// Resolved parameters of the task — i.e. the node's <see cref="Automation.Shared.Data.Graph.BaseGraphTask.InputMappingJson"/>
        /// template with context references replaced. This is NOT the data flowing in from
        /// upstream tasks (that lives in the context as <c>previous.*</c>).
        /// </summary>
        public JToken? Parameters { get; set; }
        public JToken? Output { get; set; }

        public TaskInstance? Previous { get; set; }
        /// <summary>
        /// Get the effective instance for passing through task, the graph need to be loaded (instance -> node -> automation task)
        /// </summary>
        public TaskInstance Effective => 
            Node?.AutomationTask?.Settings.IsPassingThrough == true ? 
            Previous?.Effective ?? throw new Exception("Only the start task can't have previous instance. A start instance can't be pass through.") :
            this;

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

        public TaskInstance()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public void Link(TaskInstance previous)
        {
            this.Previous = previous;
        }
    }
}
