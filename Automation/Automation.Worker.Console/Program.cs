using Automation.Models.Work;
using Automation.Plugins;
using Automation.Shared.Data;
using Automation.Worker.Control.Flow;
using Automation.Worker.Executor;
using Automation.Worker.Packages;
using Newtonsoft.Json;
using NJsonSchema;

AutomationTask delayTask = new AutomationTask()
{
    Id = Guid.NewGuid(),
    Target = new PackageClassTarget()
    {
        ClassFullName = "Automation.Plugins.TestDelay",
        Package = new PackageIdentifier()
        {
            Identifier = "Automation.Plugins",
            Version = new Version("1.0.0.0")
        }
    },
    Metadata = new ScopedMetadata()
    {
        Name = "Delay"
    },
    InputSchema = JsonSchema.FromType<TestDelayParameters>(),
    OutputSchema = new JsonSchema()
};
AutomationTask testTask = new AutomationTask()
{
    Id = Guid.NewGuid(),
    Target = new PackageClassTarget()
    {
        ClassFullName = "Automation.Plugins.TestTask",
        Package = new PackageIdentifier()
        {
            Identifier = "Automation.Plugins",
            Version = new Version("1.0.0.0")
        }
    },
    Metadata = new ScopedMetadata()
    {
        Name = "Test"
    },
    InputSchema = JsonSchema.FromType<TestParameters>(),
    OutputSchema = JsonSchema.FromType<TestResult>(),
};

Dictionary<Guid, BaseAutomationTask> tasks = new Dictionary<Guid, BaseAutomationTask>()
{
    { StartTask.AutomationTask.Id, StartTask.AutomationTask },
    { delayTask.Id, delayTask },
    { testTask.Id, testTask },
    { EndTask.AutomationTask.Id, EndTask.AutomationTask },
};

AutomationWorkflow workflow = new AutomationWorkflow()
{
    Id = Guid.NewGuid(),
    Metadata = new ScopedMetadata()
    {
        Name = "Workflow",
    }
};

GraphControl start = new GraphControl(StartTask.AutomationTask) { 
    Metadata = new ScopedMetadata() { Name = "Start" },
};

GraphTask delay = new GraphTask(delayTask)
{
    Metadata = new ScopedMetadata() { Name = "Delay" },
    InputJson = JsonConvert.SerializeObject(new TestDelayParameters() { DelayMs = 5000})
};

GraphTask test1 = new GraphTask(testTask)
{
    Metadata = new ScopedMetadata() { Name = "Test 1" },
    InputJson = JsonConvert.SerializeObject(new TestParameters() { Add = 10, Value = 1 })
};

GraphTask test2 = new GraphTask(testTask)
{
    Metadata = new ScopedMetadata() { Name = "Test 2" },
    InputJson = JsonConvert.SerializeObject(new TestParameters() { Add = 20, Value = 2 })
};

GraphControl end = new GraphControl(EndTask.AutomationTask)
{
    Metadata = new ScopedMetadata() { Name = "End" },
    TaskId = EndTask.AutomationTask.Id,
    InputJson = "{'test': '$previous.Value'}"
};


workflow.Graph.Nodes.Add(start);
workflow.Graph.Nodes.Add(delay);
workflow.Graph.Nodes.Add(test1);
workflow.Graph.Nodes.Add(test2);
workflow.Graph.Nodes.Add(end);

/*
workflow.Graph.Connect(start, delay);
workflow.Graph.Connect(delay, test1);
workflow.Graph.Connect(test1, end);
*/

workflow.Graph.Connect(start, test2);
workflow.Graph.Connect(test2, end);

workflow.Graph.Refresh(tasks);

string nuggetLocalPath = Path.Join(Directory.GetCurrentDirectory(), "nugetlocal");
LocalPackageManagement packages = new LocalPackageManagement(nuggetLocalPath);
LocalTaskExecutor executor = new LocalTaskExecutor(packages, new TaskChangeToConsole());
await executor.ExecuteAsync(workflow, null);