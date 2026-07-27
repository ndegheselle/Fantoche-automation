using System.Timers;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Timer = System.Timers.Timer;

namespace Automation.Services.Local;

/// <summary>
/// In-memory list of workers. Until the app is wired to a real worker backend it keeps a few
/// mock workers and mutates their load on a timer so the UI can demonstrate the real time
/// refresh through <see cref="WorkerUpdated"/>.
/// </summary>
public class LocalWorkersService : IWorkersService
{
    private static readonly List<WorkerInfos> _workers = [];
    private static readonly object _lock = new();
    private static readonly Random _random = new();

    private readonly Timer _timer;

    public event Action<WorkerInfos>? WorkerUpdated;
    public event Action<string>? WorkerRemoved;

    static LocalWorkersService()
    {
        var now = DateTime.UtcNow;
        _workers.AddRange(
        [
            new WorkerInfos
            {
                Id = "worker-01",
                Name = "Worker 01",
                MachineName = "srv-auto-01",
                State = EnumWorkerState.Working,
                MaxParallelTasks = 4,
                RunningTasks = 2,
                QueuedTasks = 3,
                MaxQueueSize = 50,
                Version = "1.0.0",
                LastHeartbeat = now,
            },
            new WorkerInfos
            {
                Id = "worker-02",
                Name = "Worker 02",
                MachineName = "srv-auto-02",
                State = EnumWorkerState.Available,
                MaxParallelTasks = 8,
                RunningTasks = 0,
                QueuedTasks = 0,
                MaxQueueSize = 100,
                Version = "1.0.0",
                LastHeartbeat = now,
            },
            new WorkerInfos
            {
                Id = "worker-03",
                Name = "Worker 03",
                MachineName = "srv-auto-03",
                State = EnumWorkerState.Updating,
                MaxParallelTasks = 4,
                RunningTasks = 0,
                QueuedTasks = 1,
                MaxQueueSize = 50,
                Version = "0.9.7",
                LastHeartbeat = now.AddSeconds(-20),
            },
            new WorkerInfos
            {
                Id = "worker-04",
                Name = "Worker 04",
                MachineName = "srv-auto-04",
                State = EnumWorkerState.Down,
                MaxParallelTasks = 2,
                RunningTasks = 0,
                QueuedTasks = 0,
                MaxQueueSize = 20,
                Version = "0.9.7",
                LastHeartbeat = now.AddMinutes(-5),
            },
        ]);
    }

    public LocalWorkersService()
    {
        // Simulate the load moving around so the workers list refreshes in real time.
        _timer = new Timer(TimeSpan.FromSeconds(4)) { AutoReset = true };
        _timer.Elapsed += OnTick;
        _timer.Start();
    }

    public Task<IEnumerable<WorkerInfos>> GetWorkersAsync()
    {
        lock (_lock)
            return Task.FromResult<IEnumerable<WorkerInfos>>(_workers.ToList());
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        WorkerInfos worker;
        lock (_lock)
        {
            // Only the live workers move around.
            var alive = _workers.Where(x => x.State != EnumWorkerState.Down).ToList();
            if (alive.Count == 0)
                return;
            worker = alive[_random.Next(alive.Count)];

            worker.RunningTasks = _random.Next(0, worker.MaxParallelTasks + 1);
            worker.QueuedTasks = _random.Next(0, 6);
            worker.LastHeartbeat = DateTime.UtcNow;

            if (worker.State != EnumWorkerState.Updating)
                worker.State = worker.RunningTasks > 0
                    ? EnumWorkerState.Working
                    : EnumWorkerState.Available;
        }

        WorkerUpdated?.Invoke(worker);
    }
}
