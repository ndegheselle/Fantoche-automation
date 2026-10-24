using Fantoche.Services.Remote;
using Fantoche.Worker;

namespace Fantoche.Worker.Service;

/// <summary>
/// A <see cref="LocalWorker"/> added to the workers of a remote supervisor : it runs the tasks the
/// supervisor dispatches to it.
/// <para>
/// Configured by : SUPERVISOR_URL, WORKER_ID, WORKER_CAPACITY (executions run at once),
/// DATA_FOLDER (the data of the worker, see <see cref="LocalWorker.InFolder"/>) and STORAGE_FOLDER
/// (the storage of the executions, shared by the workers so any of them resumes a run).
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
        string supervisorUrl = _configuration["SUPERVISOR_URL"] ?? "http://localhost:5165";
        Uri supervisorAddress = new(supervisorUrl.EndsWith('/') ? supervisorUrl : $"{supervisorUrl}/");
        string workerId = _configuration["WORKER_ID"] ?? Environment.MachineName;
        int capacity = _configuration.GetValue("WORKER_CAPACITY", Environment.ProcessorCount);
        string dataFolder = _configuration["DATA_FOLDER"] ?? "/app/data";
        string? storageFolder = _configuration["STORAGE_FOLDER"];

        await using var supervisor = new SupervisorClient(supervisorAddress);
        using var worker = LocalWorker.InFolder(supervisor, dataFolder, storageFolder);

        _logger.LogInformation("Worker [{WorkerId}] connecting to [{Supervisor}] to run up to {Capacity} executions at once.", workerId, supervisorAddress, capacity);
        await using IAsyncDisposable registration = await supervisor.AddWorkerAsync(worker, workerId, capacity, stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Stopping : the registration is disposed on the way out.
        }
    }
}
