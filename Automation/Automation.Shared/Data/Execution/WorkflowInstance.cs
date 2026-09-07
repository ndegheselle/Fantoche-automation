using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

public struct TaskInstancesProgress
{
    public IProgress<TaskNotification>? Notifications { get; set; }
    public IProgress<TaskInstance>? StateChanges { get; set; }
}

/// <summary>
/// Instance of a workflow execution. Carries both the persisted task-instance data
/// (id, state, input/output, ...) and the runtime data needed to drive the execution
/// (graph definition, shared token, child node instances, cancellation).
/// </summary>
public class WorkflowInstance : TaskInstance
{
    /// <summary>
    /// Workflow definition being executed.
    /// </summary>
    
    public AutomationWorkflow Workflow { get; }

    public JToken? GlobalContext { get; set; }
    // XXX : need to check how can I remvoe it compared to task instance
    public JToken? SharedContext { get; set; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    /// <summary>
    /// Cancellation source owned by the workflow
    /// </summary>
    public CancellationTokenSource WorkflowCts { get; } = new();

    public WorkflowInstance(AutomationWorkflow workflow)
    {
        Workflow = workflow;
        TaskId = workflow.Id;
    }
}