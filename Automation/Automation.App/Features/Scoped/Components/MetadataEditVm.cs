using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Automation.App.Base;
using Automation.App.Converters;
using Automation.App.Services;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Scoped.Components;

/// <summary>
/// View model backing <see cref="MetadataEditDialog"/>. It edits the metadata of a
/// <see cref="ScopedElement"/>; the changes are only applied to the element once the
/// dialog is confirmed (see <see cref="ScopedVm.Edit"/>).
/// </summary>
internal partial class MetadataEditVm : ViewModelBase
{
    private readonly ScopedElement _element;
    private readonly IScopedService _scopedService = ServiceProvider.Scoped;

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
    public EnumScopedType Type => _element.Metadata.Type;

    public MetadataEditVm(ScopedElement element)
    {
        _element = element;
        var metadata = element.Metadata;
        _name = metadata.Name;
        _selectedColor = metadata.Color;

        var icon = metadata.Icon;
        if (icon == null)
            ScopedTypeConverters.Icons.TryGetValue(metadata.Type, out icon);

        _selectedIcon = icon;
        _isReadOnly = metadata.IsReadOnly;
        Tags = new ObservableCollection<string>(metadata.Tags);
    }

    /// <summary>
    /// Applies the edited values back onto the element metadata and returns it.
    /// </summary>
    public ScopedMetadata Build()
    {
        var metadata = _element.Metadata;
        metadata.Name = Name;
        metadata.Color = SelectedColor;
        metadata.Icon = SelectedIcon;
        metadata.IsReadOnly = IsReadOnly;
        metadata.Tags = new ObservableCollection<string>(Tags);
        return metadata;
    }

    partial void OnNameChanged(string value) => ClearErrors(nameof(Name));

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
    private async Task Confirm()
    {
        Name = Name.Trim();

        bool isUnique = await _scopedService.IsNameUniqueAsync(
            _element.ParentId ?? Guid.Empty, Name, _element.Id);
        if (!isUnique)
        {
            AddError(nameof(Name), "An element with this name already exists in this scope.");
            return;
        }

        ServiceProvider.Dialogs.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel() => ServiceProvider.Dialogs.Close(this);
}
