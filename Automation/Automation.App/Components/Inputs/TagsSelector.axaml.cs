using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Automation.App.Components.Inputs;

[TemplatePart("PART_NewTagTextBox", typeof(TextBox))]
public class TagsSelector : TemplatedControl
{
    public static readonly StyledProperty<IList<string>?> TagsProperty =
        AvaloniaProperty.Register<TagsSelector, IList<string>?>(
            nameof(Tags),
            defaultValue: new ObservableCollection<string>());

    public static readonly StyledProperty<string?> NewTagProperty =
        AvaloniaProperty.Register<TagsSelector, string?>(nameof(NewTag));

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<TagsSelector, string?>(
            nameof(PlaceholderText), defaultValue: "Add a tag");

    public static readonly StyledProperty<bool> AllowDuplicatesProperty =
        AvaloniaProperty.Register<TagsSelector, bool>(nameof(AllowDuplicates));

    // Routed events so consumers can react without owning the logic.
    public static readonly RoutedEvent<TagChangedEventArgs> TagAddedEvent =
        RoutedEvent.Register<TagsSelector, TagChangedEventArgs>(
            nameof(TagAdded), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<TagChangedEventArgs> TagRemovedEvent =
        RoutedEvent.Register<TagsSelector, TagChangedEventArgs>(
            nameof(TagRemoved), RoutingStrategies.Bubble);

    private TextBox? _newTagTextBox;

    public TagsSelector()
    {
        AddTagCommand = new RelayCommand(_ => AddTag());
        RemoveTagCommand = new RelayCommand(p => RemoveTag(p as string));
    }

    public IList<string>? Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public string? NewTag
    {
        get => GetValue(NewTagProperty);
        set => SetValue(NewTagProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool AllowDuplicates
    {
        get => GetValue(AllowDuplicatesProperty);
        set => SetValue(AllowDuplicatesProperty, value);
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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_newTagTextBox is not null)
            _newTagTextBox.KeyDown -= OnNewTagKeyDown;

        _newTagTextBox = e.NameScope.Find<TextBox>("PART_NewTagTextBox");

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