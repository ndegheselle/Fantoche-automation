using System.Text.Json.Serialization;
using Fantoche.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Data.Execution
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
        /// The state of the shared context then reaching this instance.
        /// </summary>
        public JToken? Shared { get; set; }
        /// <summary>
        /// Resolved parameters of the task based on <see cref="Fantoche.Shared.Data.Graph.BaseGraphTask.InputTemplateJson"/>
        /// </summary>
        public JToken? Parameters { get; set; }
        public JToken? Output { get; set; }

        /// <summary>
        /// Only for the instance of a workflow : the context of the scopes the run started from, what
        /// the mappings of its nodes read as "$global.*". Stored so a resumed run reads the same one.
        /// </summary>
        public JToken? GlobalContext { get; set; }

        /// <summary>
        /// Only for the instance of a run : the application running it (its machine name), null when
        /// the supervisor dispatches it to its workers. Only that application can resume it.
        /// </summary>
        public string? ApplicationId { get; set; }

        /// <summary>
        /// Only for the instance of a run : what the definitions it started from hash to (see
        /// <see cref="RunDefinition"/>). A run is only resumed on the same definitions.
        /// </summary>
        public string? DefinitionHash { get; set; }

        /// <summary>
        /// The instances this one carries on from : the one before it, every branch that reached it
        /// for a join, none for the start of a run. Stored, unlike <see cref="Previous"/>, so a run
        /// can be walked again from its history.
        /// <para>
        /// Replaced rather than added to : a join is reported while other branches reach it.
        /// </para>
        /// </summary>
        public IReadOnlyList<Guid> PreviousIds { get; set; } = [];

        /// <summary>Only known while the instance is being driven by an executor, not persisted.</summary>
        [JsonIgnore]
        public TaskInstance? Previous { get; set; }
        /// <summary>
        /// Get the effective instance for passing through task, the graph need to be loaded (instance -> node -> automation task)
        /// </summary>
        [JsonIgnore]
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
            Previous = previous;
            PreviousIds = [previous.Id];
        }
    }
}
