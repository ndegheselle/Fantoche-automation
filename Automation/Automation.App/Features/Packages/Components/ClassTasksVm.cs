using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.App.Features.Scoped.Components;
using Automation.App.Services;
using Automation.App.Services.UI;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages.Components;

/// <summary>
/// Represents one row in the class/task management grid: a package class together with
/// every <see cref="AutomationTask"/> that already targets it (same package + version +
/// class name). Zero entries means no task exists yet.
/// </summary>
internal partial class ClassTaskRow : ObservableObject
{
    public ClassTarget ClassTarget { get; }

    // Surfaced as a flat property so the DataGridCollectionView can group by it.
    public string Dll => ClassTarget.Dll;

    [ObservableProperty] private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTask))]
    [NotifyPropertyChangedFor(nameof(TaskStatusLabel))]
    private IReadOnlyList<AutomationTask> _existingTasks;

    public bool HasTask => ExistingTasks.Count > 0;

    public string TaskStatusLabel => ExistingTasks.Count switch
    {
        0 => "",
        1 => "1 task",
        _ => $"{ExistingTasks.Count} tasks"
    };

    public ClassTaskRow(ClassTarget classTarget, IReadOnlyList<AutomationTask> existingTasks)
    {
        ClassTarget = classTarget;
        _existingTasks = existingTasks;
    }
}

/// <summary>
/// View model for <see cref="ClassTasksOverlay"/>. Displayed through the navigation overlay
/// system, it lets the user select package classes and create or update
/// <see cref="AutomationTask"/> entries for them.
/// </summary>
internal partial class ClassTasksVm : ObservableObject, INavigable
{
    private readonly IScopedService _scopedService;
    private readonly NavigationManager _navigation;
    private readonly ToastDisplay _toasts;
    private readonly PackageIdentifier _packageIdentifier;
    private readonly IReadOnlyList<ClassTarget> _classes;

    public ObservableCollection<ClassTaskRow> Rows { get; } = new();

    /// <summary>Grouped view over <see cref="Rows"/>, grouped by DLL.</summary>
    public DataGridCollectionView GroupedRows { get; }

    [ObservableProperty] private bool _isLoading;

    public ClassTasksVm(IScopedService scopedService,
        NavigationManager navigation,
        ToastDisplay toasts,
        PackageIdentifier packageIdentifier,
        IEnumerable<ClassTarget> classes)
    {
        _scopedService = scopedService;
        _navigation = navigation;
        _toasts = toasts;
        _packageIdentifier = packageIdentifier;
        _classes = classes.ToList();

        GroupedRows = new DataGridCollectionView(Rows);
        GroupedRows.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(ClassTaskRow.Dll)));
    }

    public void OnShow() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var existingTasks = await _scopedService.GetTasksByTargetAsync(
                _packageIdentifier.Id, _packageIdentifier.Version);

            var tasksByClass = existingTasks
                .Where(t => t.Target != null)
                .GroupBy(t => t.Target!.ClassFullName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<AutomationTask>)g.ToList());

            Rows.Clear();
            foreach (var cls in _classes)
            {
                tasksByClass.TryGetValue(cls.ClassFullName, out var tasks);
                Rows.Add(new ClassTaskRow(cls, tasks ?? []));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Apply()
    {
        var toUpdate = Rows.Where(r => r.IsSelected && r.HasTask).ToList();
        var toCreate = Rows.Where(r => r.IsSelected && !r.HasTask).ToList();

        if (toUpdate.Count == 0 && toCreate.Count == 0)
            return;

        if (toUpdate.Count > 0 && toCreate.Count > 0)
        {
            _toasts.Warning(
                "Mixed selection",
                "Select either classes that already have tasks (to update them) or classes without tasks (to create them) — not both at once.");
            return;
        }

        if (toUpdate.Count > 0)
        {
            int taskCount = toUpdate.Sum(r => r.ExistingTasks.Count);
            string body = taskCount == 1
                ? "This will update 1 existing task."
                : $"This will update {taskCount} existing tasks.";

            ServiceProvider.Dialogs
                .CreateDialog("Update existing tasks?", body)
                .WithPrimaryButton("Update", () => _ = UpdateAsync(toUpdate), DialogButtonStyle.Destructive)
                .WithCancelButton("Cancel")
                .WithMaxWidth(480)
                .Show();
        }
        else
        {
            var scopeVm = new ScopeSelectorVm(_scopedService);
            ServiceProvider.Dialogs
                .CreateDialog(scopeVm)
                .WithSuccessCallback(() => _ = CreateAsync(toCreate, scopeVm.SelectedScope!))
                .WithMaxWidth(520)
                .Show();
        }
    }

    private async Task UpdateAsync(List<ClassTaskRow> toUpdate)
    {
        foreach (var row in toUpdate)
        {
            foreach (var task in row.ExistingTasks)
                await _scopedService.EditAsync(task);
            row.IsSelected = false;
        }

        _toasts.Success("Tasks updated", $"Updated {toUpdate.Sum(r => r.ExistingTasks.Count)} task(s).");
    }

    private async Task CreateAsync(List<ClassTaskRow> toCreate, Scope targetScope)
    {
        foreach (var row in toCreate)
        {
            string shortName = row.ClassTarget.ClassFullName.Split('.').Last();
            var task = new AutomationTask(shortName, targetScope.Id)
            {
                Target = new PackageClassTarget(_packageIdentifier, row.ClassTarget.ClassFullName)
                {
                    Dll = row.ClassTarget.Dll
                }
            };
            var created = (AutomationTask)await _scopedService.CreateAsync(task);
            row.ExistingTasks = [..row.ExistingTasks, created];
            row.IsSelected = false;
        }

        _toasts.Success("Tasks created", $"Created {toCreate.Count} task(s) in '{targetScope.Metadata.Name}'.");
    }

    [RelayCommand]
    private void Back() => _navigation.Close(this);
}

internal class ClassTasksVmDesign : ClassTasksVm
{
    public ClassTasksVmDesign() : base(null!, null!, null!, new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") }, [])
    {
        var identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") };
        var target = new PackageClassTarget(identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" };

        Rows.Add(new ClassTaskRow(target, [
            new AutomationTask("HttpTask", Guid.NewGuid()) { Target = target },
            new AutomationTask("HttpTask (copy)", Guid.NewGuid()) { Target = target }
        ]));
        Rows.Add(new ClassTaskRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" },
            []));
        Rows.Add(new ClassTaskRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" },
            []));
    }
}
