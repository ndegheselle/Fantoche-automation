using Fantoche.App.Common;
using Fantoche.Worker.UserInterfaces;

namespace Fantoche.App.Features.UserInterfaces
{
    /// <summary>
    /// The tasks asking the user in this application, while they run : the ones its own runs reach,
    /// and the ones the supervisor hands it from the runs of its workers. Each one is held until the
    /// task gets its answer, its page shown in a window of its own whenever the user opens it.
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
        private readonly object _lock = new();
        private readonly List<Prompt> _asking = [];

        /// <summary>A task started asking, raised by the thread running it.</summary>
        public event Action<PageRequest>? Asked;

        /// <summary>A task started or stopped asking, raised by the thread running it.</summary>
        public event Action? AskingChanged;

        /// <param name="browserFolder">Where the browser showing the pages keeps its data.</param>
        public UserPrompts(string browserFolder)
        {
            _browserFolder = browserFolder;
        }

        public IDisposable Show(PageRequest request)
        {
            Prompt prompt = new() { Request = request };
            lock (_lock)
                _asking.Add(prompt);
            AskingChanged?.Invoke();
            Asked?.Invoke(request);
            return new Registration(() => Stop(prompt));
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
