using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Automation.App.Components.Inputs;

public partial class FilePicker : UserControl
{
    /// <summary>
    /// Text displayed in small font below the lead text, describing what kind
    /// of file is expected (e.g. the accepted format).
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(FilePicker),
            new PropertyMetadata(default(string)));

    /// <summary>
    /// Accepted file format as one or more extension patterns separated by a
    /// comma or semicolon (e.g. "*.nupkg", "*.zip;*.nupkg").
    /// An empty value accepts every file.
    /// </summary>
    public static readonly DependencyProperty FileFormatProperty =
        DependencyProperty.Register(nameof(FileFormat), typeof(string), typeof(FilePicker),
            new PropertyMetadata(default(string)));

    /// <summary>
    /// Whether the user can pick / drop more than one file at a time.
    /// </summary>
    public static readonly DependencyProperty AllowMultipleProperty =
        DependencyProperty.Register(nameof(AllowMultiple), typeof(bool), typeof(FilePicker),
            new PropertyMetadata(true));

    /// <summary>
    /// Command invoked when files are selected, either through the dialog or by
    /// dropping them. The parameter is an <see cref="IReadOnlyList{T}"/> of the
    /// selected files' paths.
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(FilePicker),
            new PropertyMetadata(default(ICommand)));

    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? FileFormat
    {
        get => (string?)GetValue(FileFormatProperty);
        set => SetValue(FileFormatProperty, value);
    }

    public bool AllowMultiple
    {
        get => (bool)GetValue(AllowMultipleProperty);
        set => SetValue(AllowMultipleProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public event EventHandler<FilesSelectedEventArgs>? FilesSelected;

    public FilePicker()
    {
        InitializeComponent();

        DropZone.MouseLeftButtonUp += OnMouseLeftButtonUp;

        DropZone.AllowDrop = true;
        DropZone.DragOver += OnDragOver;
        DropZone.Drop += OnDrop;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = AllowMultiple,
            Filter = BuildFilter(),
        };

        if (dialog.ShowDialog() == true)
            Notify(dialog.FileNames);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;

        if (!AllowMultiple)
            files = files.Take(1).ToArray();

        Notify(files);
    }

    private void Notify(IReadOnlyList<string> files)
    {
        var paths = files
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (paths.Count == 0)
            return;

        if (Command?.CanExecute(paths) == true)
            Command.Execute(paths);

        FilesSelected?.Invoke(this, new FilesSelectedEventArgs(paths));
    }

    private string BuildFilter()
    {
        string? format = FileFormat?.Trim();
        if (string.IsNullOrEmpty(format))
            return "All files (*.*)|*.*";

        var patterns = format
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (patterns.Count == 0)
            return "All files (*.*)|*.*";

        var joined = string.Join(";", patterns);
        return $"Accepted files ({joined})|{joined}";
    }
}

public sealed class FilesSelectedEventArgs(IReadOnlyList<string> files) : EventArgs
{
    public IReadOnlyList<string> Files { get; } = files;
}
