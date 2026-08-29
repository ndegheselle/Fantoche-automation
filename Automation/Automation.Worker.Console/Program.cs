using Automation.Shared.Data.Execution;
using Automation.Worker.Console.Scenarios;
using Automation.Worker.Executor;
using Automation.Worker.Packages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ---------------------------------------------------------------------------
// Runs every scenario against the real executor and prints what each node did.
//
// The tasks are loaded from the Automation.Plugins nuget package, so it has to be packed in the
// "nugetlocal" folder next to the executable beforehand :
//   dotnet pack Automation/Automation.Plugins -o Automation/Automation.Worker.Console/bin/Debug/net10.0/nugetlocal
// ---------------------------------------------------------------------------

string nugetLocalPath = Path.Join(AppContext.BaseDirectory, "nugetlocal");
string nugetCachePath = Path.Join(AppContext.BaseDirectory, "nugetcache");
LocalPackageManagement packages = new LocalPackageManagement(nugetLocalPath, nugetCachePath);

IScenario[] scenarios =
[
    new LinearScenario(),
    new BranchScenario(stopAtFirstEnd: false),
    new BranchScenario(stopAtFirstEnd: true),
    new LoopScenario(),
];

WorkflowExecutor executor = new WorkflowExecutor(packages);
TaskInstancesProgress progress = new TaskInstancesProgress()
{
    StateChanges = new Progress<TaskInstance>((instance) =>
        Console.WriteLine($"  {DateTime.Now:HH:mm:ss.fff} {instance.NodeName,-12} {instance.State,-12} Params: {Compact(instance.Parameters),-70} Output: {Compact(instance.Output)}")),
    Notifications = new Progress<Automation.Plugins.Shared.TaskNotification>((n) =>
        Console.WriteLine($"    [notification] {n.State}: {n.Message}"))
};

foreach (IScenario scenario in scenarios)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 100));
    Console.WriteLine($"# {scenario.Name}");
    Console.WriteLine(scenario.Description);
    Console.WriteLine(new string('=', 100));

    WorkflowInstance instance = new WorkflowInstance(scenario.Build())
    {
        Parameters = scenario.Input
    };

    try
    {
        WorkflowInstance result = await executor.ExecuteAsync(instance, progress);

        // The progress reports of a scenario can still be flushed after its last await.
        await Task.Delay(50);
        Console.WriteLine($"=> {result.State} : {Compact(result.Output)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"=> FAILED : {ex.Message}");
    }
}

// One line per report : a failed task carries its whole stack trace as output.
static string Compact(JToken? token)
{
    if (token == null)
        return "null";
    string json = token.ToString(Formatting.None);
    return json.Length > 200 ? json[..200] + "..." : json;
}
