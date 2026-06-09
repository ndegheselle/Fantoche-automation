using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Automation.App.Components.Inputs;

public partial class FilePicker : UserControl
{
    /// <summary>
    /// Text displayed in small font below the lead text, describing what kind
    /// of file is expected (e.g. the accepted format).
    /// </summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<FilePicker, string?>(nameof(Description));

    /// <summary>
    /// Accepted file format as one or more extension patterns separated by a
    /// comma or semicolon (e.g. "*.nupkg", "*.zip;*.nupkg").
    /// An empty value accepts every file.
    /// </summary>
    public static readonly StyledProperty<string?> FileFormatProperty =
        AvaloniaProperty.Register<FilePicker, string?>(nameof(FileFormat));

    /// <summary>
    /// Whether the user can pick / drop more than one file at a time.
    /// </summary>
    public static readonly StyledProperty<bool> AllowMultipleProperty =
        AvaloniaProperty.Register<FilePicker, bool>(nameof(AllowMultiple), defaultValue: true);

    /// <summary>
    /// Command invoked when files are selected, either through the dialog or by
    /// dropping them. The parameter is an <see cref="IReadOnlyList{T}"/> of the
    /// selected files' paths.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<FilePicker, ICommand?>(nameof(Command));

    public static readonly RoutedEvent<FilesSelectedEventArgs> FilesSelectedEvent =
        RoutedEvent.Register<FilePicker, FilesSelectedEventArgs>(
            nameof(FilesSelected), RoutingStrategies.Bubble);
    
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? FileFormat
    {
        get => GetValue(FileFormatProperty);
        set => SetValue(FileFormatProperty, value);
    }

    public bool AllowMultiple
    {
        get => GetValue(AllowMultipleProperty);
        set => SetValue(AllowMultipleProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public event EventHandler<FilesSelectedEventArgs> FilesSelected
    {
        add => AddHandler(FilesSelectedEvent, value);
        remove => RemoveHandler(FilesSelectedEvent, value);
    }

    public FilePicker()
    {
        InitializeComponent();

        DropZone.PointerReleased += OnPointerReleased;

        DragDrop.SetAllowDrop(DropZone, true);
        DropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DropZone.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null)
            return;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = AllowMultiple,
            FileTypeFilter = BuildFileTypes(),
        });

        if (files.Count > 0)
            Notify(files);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.Formats.Contains(DataFormat.File) == false)
            return;

        var files = e.DataTransfer.TryGetFiles().OfType<IStorageFile>().ToList();
        if (files == null)
            return;
        
        if (!AllowMultiple)
            files = files.Take(1).ToList();

        Notify(files);
    }

    private void Notify(IReadOnlyList<IStorageFile> files)
    {
        var paths = files
            .Select(f => f.TryGetLocalPath() ?? f.Path.LocalPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count == 0)
            return;

        if (Command?.CanExecute(paths) == true)
            Command.Execute(paths);

        RaiseEvent(new FilesSelectedEventArgs(FilesSelectedEvent, paths));
    }

    private List<FilePickerFileType>? BuildFileTypes()
    {
        string? format = FileFormat?.Trim();
        if (string.IsNullOrEmpty(format))
            return null;

        var patterns = format
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (patterns.Count == 0)
            return null;

        return
        [
            new("Accepted files") { Patterns = patterns }
        ];
    }
}

public sealed class FilesSelectedEventArgs(RoutedEvent routedEvent, IReadOnlyList<string> files)
    : RoutedEventArgs(routedEvent)
{
    public IReadOnlyList<string> Files { get; } = files;
}
