namespace Automation.Shared.Data.Scoped
{
    /// <summary>
    /// One node of one workflow pointing at a task : what a task is used by, so that it can be
    /// shown before editing it and so that it isn't removed from under the graphs running it.
    /// </summary>
    public class TaskUsage
    {
        /// <summary>Task or workflow the node points at.</summary>
        public Guid TaskId { get; set; }

        /// <summary>Workflow holding the node.</summary>
        public Guid WorkflowId { get; set; }
        public string WorkflowName { get; set; } = "";

        /// <summary>Node pointing at the task, named as it is within its graph.</summary>
        public Guid NodeId { get; set; }
        public string NodeName { get; set; } = "";

        public override string ToString() => $"'{NodeName}' in '{WorkflowName}'";
    }
}
