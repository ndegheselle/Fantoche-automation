using System.Timers;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Timer = System.Timers.Timer;

namespace Automation.Services.Local;

/// <summary>
/// In-memory history of task instances. Until a real persistence layer exists it keeps
/// the instances in a static list and simulates incoming executions with a timer so the
/// UI can demonstrate real-time refresh through <see cref="InstanceAdded"/> /
/// <see cref="InstanceUpdated"/>.
/// </summary>
public class LocalHistoryService : IHistoryService
{
    private static readonly List<TaskInstance> _instances = [];
    private static readonly object _lock = new();
    private static readonly Random _random = new();
    private static readonly string[] _nodeNames =
        ["Fetch files", "Parse data", "Validate rows", "Import to database", "Send report"];

    private static readonly EnumTaskState[] _finishedStates =
        [EnumTaskState.Completed, EnumTaskState.Failed, EnumTaskState.Canceled];

    private readonly Timer _timer;

    public event Action<TaskInstance>? InstanceAdded;
    public event Action<TaskInstance>? InstanceUpdated;

    static LocalHistoryService()
    {
        // Seed a few finished instances so the list isn't empty on first load.
        var now = DateTime.UtcNow;
        for (int i = 0; i < 12; i++)
        {
            var created = now.AddMinutes(-i * 7);
            _instances.Add(new TaskInstance
            {
                NodeName = _nodeNames[i % _nodeNames.Length],
                CreatedAt = created,
                FinishedAt = created.AddSeconds(_random.Next(5, 90)),
                State = _finishedStates[i % _finishedStates.Length],
            });
        }
    }

    public LocalHistoryService()
    {
        // Simulate new executions arriving so the history refreshes in real time.
        _timer = new Timer(TimeSpan.FromSeconds(5)) { AutoReset = true };
        _timer.Elapsed += OnTick;
        _timer.Start();
    }

    public Task<Paginated<TaskInstance>> SearchAsync(PaginationOptions options = default)
    {
        lock (_lock)
        {
            var ordered = _instances.OrderByDescending(x => x.CreatedAt).ToList();
            var items = ordered
                .Skip((options.Page - 1) * options.PageSize)
                .Take(options.PageSize)
                .ToList();

            return Task.FromResult(new Paginated<TaskInstance>
            {
                Items = items,
                Total = ordered.Count,
                Options = options,
            });
        }
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        // Half the time push a brand new running instance, otherwise finish the oldest
        // still-running one — covering both the added and updated events.
        TaskInstance? running = null;
        lock (_lock)
        {
            running = _instances.FirstOrDefault(x => (x.State & EnumTaskState.Finished) == 0);
        }

        if (running == null || _random.Next(2) == 0)
        {
            var instance = new TaskInstance
            {
                NodeName = _nodeNames[_random.Next(_nodeNames.Length)],
                State = EnumTaskState.Progressing,
            };

            lock (_lock)
                _instances.Add(instance);

            InstanceAdded?.Invoke(instance);
        }
        else
        {
            running.State = _finishedStates[_random.Next(_finishedStates.Length)];
            InstanceUpdated?.Invoke(running);
        }
    }
}
