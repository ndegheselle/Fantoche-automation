using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Components.Inputs;

public class TagsSelector : Control
{
    static TagsSelector()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TagsSelector),
            new FrameworkPropertyMetadata(typeof(TagsSelector)));
    }

    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(
            nameof(Tags), typeof(IList<string>), typeof(TagsSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty NewTagProperty =
        DependencyProperty.Register(
            nameof(NewTag), typeof(string), typeof(TagsSelector),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText), typeof(string), typeof(TagsSelector),
            new FrameworkPropertyMetadata("Add a tag"));

    public static readonly DependencyProperty AllowDuplicatesProperty =
        DependencyProperty.Register(
            nameof(AllowDuplicates), typeof(bool), typeof(TagsSelector),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(TagsSelector),
            new FrameworkPropertyMetadata(false));

    // Routed events so consumers can react without owning the logic.
    public static readonly RoutedEvent TagAddedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TagAdded), RoutingStrategy.Bubble,
            typeof(EventHandler<TagChangedEventArgs>), typeof(TagsSelector));

    public static readonly RoutedEvent TagRemovedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(TagRemoved), RoutingStrategy.Bubble,
            typeof(EventHandler<TagChangedEventArgs>), typeof(TagsSelector));

    private TextBox? _newTagTextBox;

    public TagsSelector()
    {
        AddTagCommand = new RelayCommand(_ => AddTag());
        RemoveTagCommand = new RelayCommand(p => RemoveTag(p as string));
    }

    public IList<string>? Tags
    {
        get => (IList<string>?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public string? NewTag
    {
        get => (string?)GetValue(NewTagProperty);
        set => SetValue(NewTagProperty, value);
    }

    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool AllowDuplicates
    {
        get => (bool)GetValue(AllowDuplicatesProperty);
        set => SetValue(AllowDuplicatesProperty, value);
    }

    /// <summary>
    /// When <c>true</c>, the tags are displayed but can no longer be added or removed.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }

    public event EventHandler<TagChangedEventArgs> TagAdded
    {
        add => AddHandler(TagAddedEvent, value);
        remove => RemoveHandler(TagAddedEvent, value);
    }

    public event EventHandler<TagChangedEventArgs> TagRemoved
    {
        add => AddHandler(TagRemovedEvent, value);
        remove => RemoveHandler(TagRemovedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_newTagTextBox is not null)
            _newTagTextBox.KeyDown -= OnNewTagKeyDown;

        _newTagTextBox = GetTemplateChild("PART_NewTagTextBox") as TextBox;

        if (_newTagTextBox is not null)
            _newTagTextBox.KeyDown += OnNewTagKeyDown;
    }

    private void OnNewTagKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddTag();
            e.Handled = true;
        }
    }

    private void AddTag()
    {
        var tag = NewTag?.Trim();
        if (string.IsNullOrEmpty(tag))
            return;

        Tags ??= new ObservableCollection<string>();

        if (!AllowDuplicates &&
            Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
        {
            NewTag = string.Empty;
            return;
        }

        Tags.Add(tag);
        NewTag = string.Empty;
        RaiseEvent(new TagChangedEventArgs(TagAddedEvent, this, tag));
    }

    private void RemoveTag(string? tag)
    {
        if (tag is null || Tags is null)
            return;

        if (Tags.Remove(tag))
            RaiseEvent(new TagChangedEventArgs(TagRemovedEvent, this, tag));
    }
}

public sealed class TagChangedEventArgs(RoutedEvent routedEvent, object source, string tag)
    : RoutedEventArgs(routedEvent, source)
{
    public string Tag { get; } = tag;
}

// Minimal ICommand so the control doesn't depend on CommunityToolkit.Mvvm.
// If you already reference it, delete this and use its RelayCommand.
internal sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
