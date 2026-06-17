using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Services.UI;

internal enum ToastSeverity
{
    Neutral,
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// A single toast displayed by the toast host.
/// </summary>
internal partial class ToastItem : ObservableObject
{
    public string Title { get; init; } = string.Empty;
    public string? Content { get; init; }
    public ToastSeverity Severity { get; init; }
}

/// <summary>
/// In-app toast queue. Replaces the ShadUI <c>ToastManager</c>; the fluent builder API is kept
/// identical so <see cref="ToastDisplay"/> and the toast host need no behavioural changes.
/// </summary>
internal class ToastManager
{
    /// <summary>Live toasts, rendered by the toast host bound in the main window.</summary>
    public ObservableCollection<ToastItem> Toasts { get; } = new();

    public ToastBuilder CreateToast(string title) => new(this, title);

    internal void Enqueue(ToastItem item, int delaySeconds)
    {
        Toasts.Add(item);

        if (delaySeconds <= 0)
            return;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delaySeconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Dismiss(item);
        };
        timer.Start();
    }

    public void Dismiss(ToastItem item) => Toasts.Remove(item);
}

/// <summary>
/// Fluent builder mirroring the ShadUI toast API (<c>CreateToast().WithContent().WithDelay().ShowX()</c>).
/// </summary>
internal class ToastBuilder
{
    private readonly ToastManager _manager;
    private readonly string _title;
    private string? _content;
    private int _delaySeconds;

    public ToastBuilder(ToastManager manager, string title)
    {
        _manager = manager;
        _title = title;
    }

    public ToastBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public ToastBuilder WithDelay(int seconds)
    {
        _delaySeconds = seconds;
        return this;
    }

    public void Show() => Emit(ToastSeverity.Neutral);
    public void ShowInfo() => Emit(ToastSeverity.Info);
    public void ShowWarning() => Emit(ToastSeverity.Warning);
    public void ShowError() => Emit(ToastSeverity.Error);
    public void ShowSuccess() => Emit(ToastSeverity.Success);

    private void Emit(ToastSeverity severity)
    {
        _manager.Enqueue(
            new ToastItem { Title = _title, Content = _content, Severity = severity },
            _delaySeconds);
    }
}
