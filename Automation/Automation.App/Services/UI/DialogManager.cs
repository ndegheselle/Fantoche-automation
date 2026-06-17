using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Services.UI;

/// <summary>Result passed when closing a dialog.</summary>
internal class CloseDialogOptions
{
    public bool Success { get; init; }
}

/// <summary>Visual emphasis of a message-dialog button.</summary>
internal enum DialogButtonStyle
{
    Primary,
    Secondary,
    Destructive
}

/// <summary>
/// Hosts a single modal dialog at a time. Replaces the ShadUI <c>DialogManager</c>; the fluent
/// builder API used by the view models is preserved so call sites are unchanged. The active dialog
/// content (a content view model or a <see cref="MessageDialogVm"/>) is rendered by the dialog host
/// bound in the main window via <see cref="CurrentContent"/>.
/// </summary>
internal partial class DialogManager : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private bool _dismissible;

    [ObservableProperty]
    private double _maxWidth = 560;

    private object? _key;
    private Action? _onSuccess;
    private Action? _onCancel;

    /// <summary>Opens a custom dialog rendering the given content <paramref name="viewModel"/>.</summary>
    public ContentDialogBuilder CreateDialog(object viewModel) => new(this, viewModel);

    /// <summary>Opens a simple confirmation dialog with a title and message.</summary>
    public MessageDialogBuilder CreateDialog(string title, string message) => new(this, title, message);

    internal void Open(object key, object content, Action? onSuccess, Action? onCancel, bool dismissible, double maxWidth)
    {
        _key = key;
        _onSuccess = onSuccess;
        _onCancel = onCancel;
        Dismissible = dismissible;
        MaxWidth = maxWidth;
        CurrentContent = content;
        IsOpen = true;
    }

    /// <summary>Closes the dialog identified by <paramref name="key"/>, running its success/cancel callback.</summary>
    public void Close(object key, CloseDialogOptions? options = null)
    {
        if (!IsOpen || !ReferenceEquals(key, _key))
            return;

        bool success = options?.Success ?? false;
        Action? callback = success ? _onSuccess : _onCancel;

        Hide();
        callback?.Invoke();
    }

    /// <summary>Dismisses the current dialog (treated as a cancel). Used by the backdrop.</summary>
    [RelayCommand]
    public void Dismiss()
    {
        if (!IsOpen || !Dismissible || _key is null)
            return;
        Close(_key);
    }

    private void Hide()
    {
        IsOpen = false;
        CurrentContent = null;
        _key = null;
        _onSuccess = null;
        _onCancel = null;
    }
}

/// <summary>Fluent builder for a custom content dialog.</summary>
internal class ContentDialogBuilder
{
    private readonly DialogManager _manager;
    private readonly object _viewModel;
    private Action? _onSuccess;
    private Action? _onCancel;
    private bool _dismissible;
    private double _maxWidth = 560;

    public ContentDialogBuilder(DialogManager manager, object viewModel)
    {
        _manager = manager;
        _viewModel = viewModel;
    }

    public ContentDialogBuilder WithSuccessCallback(Action callback)
    {
        _onSuccess = callback;
        return this;
    }

    public ContentDialogBuilder WithCancelCallback(Action callback)
    {
        _onCancel = callback;
        return this;
    }

    public ContentDialogBuilder Dismissible()
    {
        _dismissible = true;
        return this;
    }

    public ContentDialogBuilder WithMaxWidth(double maxWidth)
    {
        _maxWidth = maxWidth;
        return this;
    }

    public void Show() => _manager.Open(_viewModel, _viewModel, _onSuccess, _onCancel, _dismissible, _maxWidth);
}

/// <summary>Fluent builder for a simple confirmation (title/message + buttons) dialog.</summary>
internal class MessageDialogBuilder
{
    private readonly DialogManager _manager;
    private readonly MessageDialogVm _model;
    private Action? _onSuccess;
    private bool _dismissible;
    private double _maxWidth = 560;

    public MessageDialogBuilder(DialogManager manager, string title, string message)
    {
        _manager = manager;
        _model = new MessageDialogVm(title, message);
    }

    public MessageDialogBuilder WithPrimaryButton(string text, Action callback, DialogButtonStyle style = DialogButtonStyle.Primary)
    {
        _onSuccess = callback;
        _model.Buttons.Add(new DialogButton(text, style, new RelayCommand(() =>
            _manager.Close(_model, new CloseDialogOptions { Success = true }))));
        return this;
    }

    public MessageDialogBuilder WithCancelButton(string text)
    {
        _model.Buttons.Add(new DialogButton(text, DialogButtonStyle.Secondary, new RelayCommand(() =>
            _manager.Close(_model))));
        return this;
    }

    public MessageDialogBuilder Dismissible()
    {
        _dismissible = true;
        return this;
    }

    public MessageDialogBuilder WithMaxWidth(double maxWidth)
    {
        _maxWidth = maxWidth;
        return this;
    }

    public void Show() => _manager.Open(_model, _model, _onSuccess, null, _dismissible, _maxWidth);
}

/// <summary>View model backing a simple confirmation dialog.</summary>
internal partial class MessageDialogVm : ObservableObject
{
    public MessageDialogVm(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Title { get; }
    public string Message { get; }
    public ObservableCollection<DialogButton> Buttons { get; } = new();
}

/// <summary>A button shown in a <see cref="MessageDialogVm"/>.</summary>
internal class DialogButton
{
    public DialogButton(string text, DialogButtonStyle style, IRelayCommand command)
    {
        Text = text;
        Style = style;
        Command = command;
    }

    public string Text { get; }
    public DialogButtonStyle Style { get; }
    public IRelayCommand Command { get; }
}
