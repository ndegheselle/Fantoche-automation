namespace Fantoche.Worker.Tests;

/// <summary>
/// Progress reported synchronously rather than through <see cref="Progress{T}"/> : a report posted
/// to the thread pool can land after the run returned, which no assertion could wait for.
/// </summary>
internal sealed class Sink<T> : IProgress<T>
{
    private readonly Action<T> _report;
    public Sink(Action<T> report) => _report = report;
    public void Report(T value) => _report(value);
}
