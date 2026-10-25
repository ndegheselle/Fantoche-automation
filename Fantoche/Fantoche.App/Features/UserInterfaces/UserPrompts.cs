using Fantoche.App.Common;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Fantoche.Worker.UserInterfaces;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The tasks asking the user in this application, while they run : the ones its own runs reach,
    /// and the ones its user takes from the runs of the workers (see <see cref="AnswerAsync"/>). Each
    /// one is held until the task gets its answer, its page shown in a window of its own whenever
    /// the user opens it : nothing shows up unasked.
    /// </summary>
    public sealed class UserPrompts : IPageHost
    {
        private sealed class Prompt
        {
            public required PageRequest Request { get; init; }
            /// <summary>The window showing the page, null while it is closed. Read and written on the UI thread.</summary>
            public PageWindow? Window { get; set; }
            /// <summary>Closed once the task stops asking, whether the page is open or not.</summary>
            public bool IsOver { get; set; }
        }

        private readonly string _browserFolder;
        private readonly IHistoryService _history;
        private readonly Func<Guid, Task<bool>> _takeAsync;
        private readonly object _lock = new();
        private readonly List<Prompt> _asking = [];
        /// <summary>The tasks the user took, whose page opens as soon as they ask.</summary>
        private readonly HashSet<Guid> _taken = [];

        /// <summary>
        /// A task of a run of this application started asking, raised by the thread running it. A
        /// task the user took opens its page instead.
        /// </summary>
        public event Action<PageRequest>? Asked;

        /// <summary>A task started or stopped asking, raised by the thread running it.</summary>
        public event Action? AskingChanged;

        /// <param name="browserFolder">Where the browser showing the pages keeps its data.</param>
        /// <param name="takeAsync">
        /// Take a task asking the user waiting at the supervisor, false when another application took
        /// it first (see <see cref="Worker.IWorkerRegistration.TakeAsync"/>).
        /// </param>
        public UserPrompts(string browserFolder, IHistoryService history, Func<Guid, Task<bool>> takeAsync)
        {
            _browserFolder = browserFolder;
            _history = history;
            _takeAsync = takeAsync;
        }

        public IDisposable Show(PageRequest request)
        {
            Prompt prompt = new() { Request = request };
            bool isTaken;
            lock (_lock)
            {
                _asking.Add(prompt);
                isTaken = _taken.Remove(request.Instance.Id);
            }
            AskingChanged?.Invoke();

            if (isTaken)
                Ui.Post(() => _ = OpenAsync(request));
            else
                Asked?.Invoke(request);
            return new Registration(() => Stop(prompt));
        }

        /// <summary>
        /// Answer [instance] : the task itself, or a run holding tasks asking the user. A task asking in
        /// this application opens its page ; one waiting at the supervisor is taken first, its page
        /// opening once the task asks, this application running it.
        /// </summary>
        /// <exception cref="ExecutionException">Nothing asks anymore, or another application took every task asking.</exception>
        public async Task AnswerAsync(TaskInstance instance)
        {
            IReadOnlyList<PageRequest> asking = AskingFor(instance.Id);
            if (asking.Count > 0)
            {
                await OpenAsync(asking[0]);
                return;
            }

            // Marked taken before it is : the task may ask before the supervisor answers.
            IReadOnlyList<TaskInstance> descendants = await _history.GetDescendantsAsync(instance.Id);
            IEnumerable<TaskInstance> waiting = descendants.Prepend(instance)
                .Where(x => x.State == EnumTaskState.AwaitingUser && x.ParentInstanceId != null);
            foreach (TaskInstance node in waiting)
            {
                lock (_lock)
                    _taken.Add(node.Id);
                if (await _takeAsync(node.Id))
                    return;
                lock (_lock)
                    _taken.Remove(node.Id);
            }

            throw new ExecutionException($"Nothing of '{instance.NodeName}' waits for you anymore : it is answered from another application.");
        }

        /// <summary>
        /// The tasks asking for [instanceId] : the task itself, a run or a nested run holding it.
        /// </summary>
        public IReadOnlyList<PageRequest> AskingFor(Guid instanceId)
        {
            lock (_lock)
            {
                return [.. _asking
                    .Select(x => x.Request)
                    .Where(x => x.Instance.Id == instanceId || x.RunId == instanceId || x.Instance.ParentInstanceId == instanceId)];
            }
        }

        /// <summary>
        /// Show the page of [request], brought to the front when already open. Returns once the window
        /// closes, answered or not : closed unanswered, the task asks on.
        /// </summary>
        public async Task OpenAsync(PageRequest request)
        {
            Prompt? prompt;
            lock (_lock)
                prompt = _asking.FirstOrDefault(x => x.Request == request);
            if (prompt == null || prompt.IsOver)
                return;

            if (prompt.Window != null)
            {
                prompt.Window.Activate();
                await prompt.Window.Dismissed;
                return;
            }

            prompt.Window = PageWindow.Open(request, _browserFolder);
            try
            {
                await prompt.Window.Dismissed;
            }
            finally
            {
                prompt.Window = null;
            }
        }

        /// <summary>The task stopped asking : answered, canceled or failed. Its page closes if open.</summary>
        private void Stop(Prompt prompt)
        {
            lock (_lock)
                _asking.Remove(prompt);
            Ui.Post(() =>
            {
                prompt.IsOver = true;
                prompt.Window?.Close();
            });
            AskingChanged?.Invoke();
        }

        private sealed class Registration(Action dispose) : IDisposable
        {
            private Action? _dispose = dispose;

            public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}
