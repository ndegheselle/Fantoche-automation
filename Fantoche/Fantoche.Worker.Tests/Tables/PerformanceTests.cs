using System.Diagnostics;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The practical workflow on large files, run by hand :
/// <c>dotnet test --filter "FullyQualifiedName~PerformanceTests"</c>, the number of orders read from
/// FANTOCHE_PERF_ORDERS (1 000 000 by default). Writes how long each node took, and fails only when
/// the run does : how fast is fast enough depends on the machine.
/// </summary>
[TestFixture]
[Explicit("Generates and processes large files, run by hand.")]
internal sealed class PerformanceTests
{
    private const int Customers = 100_000;

    [Test]
    public async Task LargeFiles_RunThroughTheWholeWorkflow()
    {
        int orders = int.TryParse(Environment.GetEnvironmentVariable("FANTOCHE_PERF_ORDERS"), out int asked) ? asked : 1_000_000;
        using CsvFiles files = new();
        string customersPath = Path.Combine(files.Folder, "customers.csv");
        string ordersPath = Path.Combine(files.Folder, "orders.csv");
        CustomersOrdersWorkflow.WriteCustomers(customersPath, Customers);
        CustomersOrdersWorkflow.WriteOrders(ordersPath, orders, Customers);

        Stopwatch watch = Stopwatch.StartNew();
        WorkflowRun run = await CustomersOrdersWorkflow.Build().RunAsync(new { Customers = customersPath, Orders = ordersPath });
        watch.Stop();

        TestContext.Out.WriteLine($"{Customers} customers, {orders} orders : {watch.Elapsed.TotalSeconds:0.0} s");
        foreach (TaskInstance node in run.Stored.OrderBy(x => x.CreatedAt))
            TestContext.Out.WriteLine($"  {node.NodeName,-20} {(node.FinishedAt - node.CreatedAt)?.TotalSeconds:0.00} s");

        Assert.That(run.Instance.State, Is.EqualTo(EnumTaskState.Completed), string.Join("\n", run.Messages));
    }
}
