using Fantoche.Worker.Executor;
using Fantoche.Worker.Executor.Remote;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker.Service;

/// <summary>
/// Run the tasks the supervisor sends, with the packages and the storage of this machine.
/// <para>
/// Configured by : SUPERVISOR_URL, WORKER_ID, WORKER_CAPACITY (executions run at once),
/// DATA_FOLDER (the cache of the packages) and SHARED_FOLDER (the "nuget" packages and the
/// "storage", shared with the supervisor and the other workers).
/// </para>
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Uri supervisor = new(_configuration["SUPERVISOR_URL"] ?? "http://localhost:5165");
        string workerId = _configuration["WORKER_ID"] ?? Environment.MachineName;
        int capacity = _configuration.GetValue("WORKER_CAPACITY", Environment.ProcessorCount);
        string dataFolder = _configuration["DATA_FOLDER"] ?? "/app/data";
        string sharedFolder = _configuration["SHARED_FOLDER"] ?? "/app/shared";

        var packages = new LocalPackageManagement(Path.Combine(sharedFolder, "nuget"), Path.Combine(dataFolder, "cache"));
        var storage = new StorageFolder(Path.Combine(sharedFolder, "storage"));
        using var executor = new LocalTaskExecutor(packages, storage);
        await using var connection = new WorkerConnection(supervisor, workerId, capacity, executor);

        _logger.LogInformation("Worker [{WorkerId}] connecting to [{Supervisor}] to run up to {Capacity} executions at once.", workerId, supervisor, capacity);
        await connection.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            await connection.StopAsync();
        }
    }
}
