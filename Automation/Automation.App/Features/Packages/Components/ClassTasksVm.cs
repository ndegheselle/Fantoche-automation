using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.App.Base;
using Automation.App.Features.Scoped.Components;
using Automation.App.Services;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages.Components;

/// <summary>
/// Represents one row in the class/task management grid: a package class with its
/// optional already-existing <see cref="AutomationTask"/>.
/// </summary>
internal partial class ClassTaskRow : ObservableObject
{
    public ClassTarget ClassTarget { get; }

    // Surfaced as a flat property so the DataGridCollectionView can group by it.
    public string Dll => ClassTarget.Dll;

    [ObservableProperty] private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTask))]
    private AutomationTask? _existingTask;

    public bool HasTask => ExistingTask != null;

    public ClassTaskRow(ClassTarget classTarget, AutomationTask? existingTask)
    {
        ClassTarget = classTarget;
        _existingTask = existingTask;
    }
}

/// <summary>
/// View model for <see cref="ClassTasksDialog"/>. Lets the user select package classes and
/// create or update <see cref="AutomationTask"/> entries for them.
/// </summary>
internal partial class ClassTasksVm : ViewModelBase
{
    private readonly IScopedService _scopedService;
    private readonly PackageIdentifier _packageIdentifier;

    public ObservableCollection<ClassTaskRow> Rows { get; } = new();

    /// <summary>Grouped view over <see cref="Rows"/>, grouped by DLL.</summary>
    public DataGridCollectionView GroupedRows { get; }

    [ObservableProperty] private bool _isLoading;

    public ClassTasksVm(IScopedService? scopedService, PackageIdentifier packageIdentifier, IEnumerable<ClassTarget> classes)
    {
        _scopedService = scopedService!;
        _packageIdentifier = packageIdentifier;

        GroupedRows = new DataGridCollectionView(Rows);
        GroupedRows.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(ClassTaskRow.Dll)));

        if (scopedService != null)
            _ = LoadAsync(classes);
    }

    private async Task LoadAsync(IEnumerable<ClassTarget> classes)
    {
        IsLoading = true;
        try
        {
            var existingTasks = await _scopedService.GetTasksByTargetAsync(
                _packageIdentifier.Id, _packageIdentifier.Version);

            Rows.Clear();
            foreach (var cls in classes)
            {
                var existing = existingTasks.FirstOrDefault(t =>
                    string.Equals(t.Target?.ClassFullName, cls.ClassFullName, StringComparison.Ordinal));
                Rows.Add(new ClassTaskRow(cls, existing));
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

        if (toUpdate.Count > 0)
        {
            string body = toUpdate.Count == 1
                ? "This will update 1 existing task."
                : $"This will update {toUpdate.Count} existing tasks.";

            ServiceProvider.Dialogs
                .CreateDialog("Update existing tasks?", body)
                .WithPrimaryButton("Update", () => PickScopeAndApply(toCreate, toUpdate), DialogButtonStyle.Destructive)
                .WithCancelButton("Cancel")
                .WithMaxWidth(480)
                .Show();
        }
        else
        {
            PickScopeAndApply(toCreate, []);
        }
    }

    private void PickScopeAndApply(List<ClassTaskRow> toCreate, List<ClassTaskRow> toUpdate)
    {
        if (toCreate.Count > 0)
        {
            var scopeVm = new ScopeSelectorVm(_scopedService);
            ServiceProvider.Dialogs
                .CreateDialog(scopeVm)
                .WithSuccessCallback(() => _ = ExecuteAsync(toCreate, toUpdate, scopeVm.SelectedScope!))
                .WithMaxWidth(520)
                .Show();
        }
        else
        {
            _ = ExecuteAsync([], toUpdate, null);
        }
    }

    private async Task ExecuteAsync(List<ClassTaskRow> toCreate, List<ClassTaskRow> toUpdate, Scope? targetScope)
    {
        foreach (var row in toCreate)
        {
            string shortName = row.ClassTarget.ClassFullName.Split('.').Last();
            var task = new AutomationTask(shortName, targetScope!.Id)
            {
                Target = new PackageClassTarget(_packageIdentifier, row.ClassTarget.ClassFullName)
                {
                    Dll = row.ClassTarget.Dll
                }
            };
            var created = (AutomationTask)await _scopedService.CreateAsync(task);
            row.ExistingTask = created;
            row.IsSelected = false;
        }

        foreach (var row in toUpdate)
        {
            await _scopedService.EditAsync(row.ExistingTask!);
            row.IsSelected = false;
        }

        ServiceProvider.Dialogs.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel() => ServiceProvider.Dialogs.Close(this);
}

internal class ClassTasksVmDesign : ClassTasksVm
{
    public ClassTasksVmDesign() : base(null, new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") }, [])
    {
        var identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") };

        var existingTask = new AutomationTask("HttpTask", Guid.NewGuid())
        {
            Target = new PackageClassTarget(identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" }
        };

        Rows.Add(new ClassTaskRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" },
            existingTask));
        Rows.Add(new ClassTaskRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" },
            null));
        Rows.Add(new ClassTaskRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" },
            null));
    }
}
