using System.Collections.ObjectModel;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Workflows.Elements;

/// <summary>
/// View model backing <see cref="MetadataEditDialog"/>. It edits a working copy of a
/// <see cref="ScopedMetadata"/>; the changes are only applied to the original element once the
/// dialog is confirmed (see <see cref="ScopedVM.Edit"/>).
/// </summary>
internal partial class MetadataEditVM : ObservableObject
{
    private readonly ScopedMetadata _metadata;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string? _selectedColor;

    [ObservableProperty]
    private string? _selectedIcon;

    [ObservableProperty]
    private bool _isReadOnly;

    [ObservableProperty]
    private string _newTag = "";

    public ObservableCollection<string> Tags { get; }

    /// <summary>Type of the edited element, displayed as a non editable header.</summary>
    public EnumScopedType Type => _metadata.Type;

    public MetadataEditVM(ScopedMetadata metadata)
    {
        _metadata = metadata;
        _name = metadata.Name;
        _selectedColor = metadata.Color;
        _selectedIcon = metadata.Icon;
        _isReadOnly = metadata.IsReadOnly;
        Tags = new ObservableCollection<string>(metadata.Tags);
    }

    /// <summary>
    /// Applies the edited values back onto the working metadata and returns it.
    /// </summary>
    public ScopedMetadata Build()
    {
        _metadata.Name = Name;
        _metadata.Color = SelectedColor;
        _metadata.Icon = SelectedIcon;
        _metadata.IsReadOnly = IsReadOnly;
        _metadata.Tags = new ObservableCollection<string>(Tags);
        return _metadata;
    }

    [RelayCommand]
    private void AddTag()
    {
        string tag = NewTag.Trim();
        if (string.IsNullOrEmpty(tag) || Tags.Contains(tag))
            return;

        Tags.Add(tag);
        NewTag = "";
    }

    [RelayCommand]
    private void RemoveTag(string tag) => Tags.Remove(tag);

    [RelayCommand]
    private void Confirm() => ServiceProvider.Dialogs.Close(this, new CloseDialogOptions { Success = true });

    [RelayCommand]
    private void Cancel() => ServiceProvider.Dialogs.Close(this);
}
