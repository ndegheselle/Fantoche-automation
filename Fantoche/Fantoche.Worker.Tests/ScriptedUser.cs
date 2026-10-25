using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests;

/// <summary>
/// The user of a test, in place of the pages of the application : each page a task asks is answered
/// the way the test says, calling the methods of the task the way a page would.
/// </summary>
internal sealed class ScriptedUser : IPageHost
{
    private readonly Func<PageRequest, CancellationToken, Task<JToken?>> _answer;
    private readonly object _lock = new();
    private readonly List<PageRequest> _asked = [];
    private readonly List<string> _refusals = [];
    private readonly TaskCompletionSource<PageRequest> _firstAsked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _open;

    /// <param name="answer">What a page is answered with, null to leave it unanswered.</param>
    public ScriptedUser(Func<PageRequest, CancellationToken, Task<JToken?>> answer)
    {
        _answer = answer;
    }

    /// <summary>A user answering every page with [answer].</summary>
    public static ScriptedUser Answering(object answer) => new((_, _) => Task.FromResult<JToken?>(JToken.FromObject(answer)));

    /// <summary>A user never answering : the tasks ask until they are stopped.</summary>
    public static ScriptedUser Away() => new((_, _) => Task.FromResult<JToken?>(null));

    /// <summary>Every page asked, in order.</summary>
    public IReadOnlyList<PageRequest> Asked
    {
        get { lock (_lock) return [.. _asked]; }
    }

    /// <summary>Why the answers were refused, in order.</summary>
    public IReadOnlyList<string> Refusals
    {
        get { lock (_lock) return [.. _refusals]; }
    }

    /// <summary>How many pages are still asked : held, neither answered nor stopped.</summary>
    public int Open => Volatile.Read(ref _open);

    /// <summary>The first page asked, once it is.</summary>
    public Task<PageRequest> FirstAskedAsync() => _firstAsked.Task.WaitAsync(TimeSpan.FromSeconds(10));

    public IDisposable Show(PageRequest request)
    {
        lock (_lock)
            _asked.Add(request);
        Interlocked.Increment(ref _open);
        _firstAsked.TrySetResult(request);

        CancellationTokenSource stopped = new();
        _ = Task.Run(() => AnswerAsync(request, stopped.Token));
        return new Registration(() =>
        {
            stopped.Cancel();
            Interlocked.Decrement(ref _open);
        });
    }

    private async Task AnswerAsync(PageRequest request, CancellationToken stopped)
    {
        JToken? answer = await _answer(request, stopped);
        if (answer == null || stopped.IsCancellationRequested)
            return;

        string? refusal = request.Answer(answer);
        if (refusal != null)
        {
            lock (_lock)
                _refusals.Add(refusal);
        }
    }

    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
