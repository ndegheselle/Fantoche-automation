using Automation.Plugins;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Executor;
using Automation.Worker.Packages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

// ---------------------------------------------------------------------------
// Task definitions (the reusable "blueprints" the graph nodes point to).
// ---------------------------------------------------------------------------

AutomationTask MakeTask(string className, string name, JsonSchema? input, JsonSchema? output, bool passThrough = false)
    => new AutomationTask()
    {
        Id = Guid.NewGuid(),
        Target = new PackageClassTarget()
        {
            Dll = "Automation.Plugins",
            ClassFullName = className,
            Package = new PackageIdentifier()
            {
                Identifier = "Automation.Plugins",
                Version = new Version("1.0.0.0")
            }
        },
        Metadata = new ScopedMetadata() { Name = name },
        InputSchema = input,
        OutputSchema = output,
        Settings = new TaskSettings() { IsPassingThrough = passThrough }
    };

AutomationTask testTask = MakeTask(
    "Automation.Plugins.TestTask", "Test",
    JsonSchema.FromType<TestParameters>(), JsonSchema.FromType<TestResult>());

AutomationTask conditionalTask = MakeTask(
    "Automation.Plugins.ConditionalTask", "Conditional",
    JsonSchema.FromType<ConditionalParameters>(), new JsonSchema(), passThrough: true);

AutomationTask passThroughTask = MakeTask(
    "Automation.Plugins.PassThroughTask", "PassThrough",
    JsonSchema.FromType<PassThroughParameters>(), new JsonSchema(), passThrough: true);

Dictionary<Guid, BaseAutomationTask> tasks = new()
{
    { AutomationControl.StartTask.Id, AutomationControl.StartTask },
    { testTask.Id, testTask },
    { conditionalTask.Id, conditionalTask },
    { passThroughTask.Id, passThroughTask },
    { AutomationControl.EndTask.Id, AutomationControl.EndTask },
};

// ---------------------------------------------------------------------------
// Workflow definition with input and output schemas.
// ---------------------------------------------------------------------------

AutomationWorkflow workflow = new AutomationWorkflow()
{
    Id = Guid.NewGuid(),
    Metadata = new ScopedMetadata() { Name = "Workflow" },
    InputSchema = JsonSchema.FromType<WorkflowInput>(),
    OutputSchema = JsonSchema.FromType<TestResult>(),
};

// ---------------------------------------------------------------------------
// Graph nodes.
//
//   Start ─┬─> TestA ─> PassThrough ─┐
//          │                         ├─(wait all)─> WaitAll ─> End
//          ├─> TestB ────────────────┘
//          │
//          └─> Gate (deactivates output) ─X─> End   (branch dies, never reached)
// ---------------------------------------------------------------------------

GraphControl start = new GraphControl(AutomationControl.StartTask)
{
    Metadata = new ScopedMetadata() { Name = "Start" },
};

GraphTask testA = new GraphTask(testTask)
{
    Metadata = new ScopedMetadata() { Name = "TestA" },
    ParametersJson = JsonConvert.SerializeObject(new { Message = "a", Value = "$previous.StartValue", Add = 10 })
};

GraphTask testB = new GraphTask(testTask)
{
    Metadata = new ScopedMetadata() { Name = "TestB" },
    ParametersJson = JsonConvert.SerializeObject(new { Message = "b", Value = "$previous.StartValue", Add = 20 })
};

GraphTask passThrough = new GraphTask(passThroughTask)
{
    Metadata = new ScopedMetadata() { Name = "PassThrough" },
    ParametersJson = JsonConvert.SerializeObject(new PassThroughParameters() { Label = "after TestA" })
};

GraphTask gate = new GraphTask(conditionalTask)
{
    Metadata = new ScopedMetadata() { Name = "Gate" },
    ParametersJson = JsonConvert.SerializeObject(new ConditionalParameters() { TestDeactivate = true })
};

// Merges the PassThrough branch (resolves to TestA) and the TestB branch.
GraphTask waitAll = new GraphTask(testTask)
{
    Metadata = new ScopedMetadata() { Name = "WaitAll" },
    Settings = new GraphTaskSettings() { IsWaitingAllInputs = true },
    ParametersJson = JsonConvert.SerializeObject(new
    {
        Message = "merged",
        Value = "$previous.TestA.Value",
        Add = "$previous.TestB.Value"
    })
};

GraphControl end = new GraphControl(AutomationControl.EndTask)
{
    Metadata = new ScopedMetadata() { Name = "End" },
};

workflow.Graph.Nodes.Add(start);
workflow.Graph.Nodes.Add(testA);
workflow.Graph.Nodes.Add(testB);
workflow.Graph.Nodes.Add(passThrough);
workflow.Graph.Nodes.Add(gate);
workflow.Graph.Nodes.Add(waitAll);
workflow.Graph.Nodes.Add(end);

workflow.Graph.Connect(start, testA);
workflow.Graph.Connect(start, testB);
workflow.Graph.Connect(start, gate);

workflow.Graph.Connect(testA, passThrough);
workflow.Graph.Connect(passThrough, waitAll);
workflow.Graph.Connect(testB, waitAll);

workflow.Graph.Connect(waitAll, end);
workflow.Graph.Connect(gate, end);

workflow.Graph.Refresh(tasks);

// ---------------------------------------------------------------------------
// Execution.
// ---------------------------------------------------------------------------

string nuggetLocalPath = Path.Join(Directory.GetCurrentDirectory(), "nugetlocal");
LocalPackageManagement packages = new LocalPackageManagement(nuggetLocalPath);

LocalWorkflowExecutor executor = new LocalWorkflowExecutor(packages);
TaskInstancesProgress progress = new TaskInstancesProgress()
{
    StateChanges = new Progress<TaskInstance>((instance) =>
        Console.WriteLine($"{instance.CreatedAt:HH:mm:ss.fff} {instance.NodeName,-12} {instance.State,-12} Params: {instance.Parameters}  Output: {instance.Output}")),
    Notifications = new Progress<Automation.Plugins.Shared.TaskNotification>((n) =>
        Console.WriteLine($"  [notification] {n.State}: {n.Message}"))
};

JToken input = JToken.FromObject(new WorkflowInput() { StartValue = 1 });

var result = await executor.ExecuteAsync(workflow, input, progress: progress);

Console.WriteLine();
Console.WriteLine($"Workflow finished with state {result.State}");
Console.WriteLine($"Workflow output: {result.Output}");

// ---------------------------------------------------------------------------
// Workflow input / output schema types.
// ---------------------------------------------------------------------------

public class WorkflowInput
{
    public int StartValue { get; set; }
}
