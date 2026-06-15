using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.App.Services;
using Automation.App.Services.UI;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Packages.Components;

/// <summary>
/// One row of the "new classes" grid: a package class that has no <see cref="AutomationTask"/>
/// targeting it yet. Selecting it creates a new task in the chosen scope.
/// </summary>
internal partial class NewClassRow : ObservableObject
{
    public ClassTarget ClassTarget { get; }

    public string ClassFullName => ClassTarget.ClassFullName;
    public string Dll => ClassTarget.Dll;

    [ObservableProperty] private bool _isSelected;

    public NewClassRow(ClassTarget classTarget) => ClassTarget = classTarget;
}

/// <summary>
/// One row of the "existing tasks" grid: an <see cref="AutomationTask"/> already linked to the
/// package. Selecting it updates the task's target to the newly added version.
/// </summary>
internal partial class ExistingTaskRow : ObservableObject
{
    public AutomationTask Task { get; }

    public string Name => Task.Metadata.Name;
    public string ClassFullName => Task.Target?.ClassFullName ?? "";
    public Version? CurrentVersion => Task.Target?.Package.Version;

    [ObservableProperty] private bool _isSelected;

    public ExistingTaskRow(AutomationTask task) => Task = task;
}

/// <summary>
/// View model for <see cref="PackageTasksOverlay"/>. Shown right after a package is added, it lets
/// the user create tasks for the package's new classes and/or update tasks that already target an
/// older version of the package.
/// </summary>
internal partial class PackageTasksVm : ObservableObject, INavigable
{
    private readonly IScopedService _scopedService;
    private readonly IPackagesService _packagesService;
    private readonly NavigationManager _navigation;
    private readonly ToastDisplay _toasts;
    private readonly PackageInfos _package;

    /// <summary>Classes of the package that don't have an associated task yet.</summary>
    public ObservableCollection<NewClassRow> NewClasses { get; } = new();

    /// <summary>Tasks that already target the package, to be updated to the new version.</summary>
    public ObservableCollection<ExistingTaskRow> ExistingTasks { get; } = new();

    /// <summary>Version of the package that was just added (the update target).</summary>
    public Version TargetVersion => _package.Identifier.Version;

    /// <summary>Scope in which new tasks are created; defaults to the root scope.</summary>
    [ObservableProperty] private Scope? _selectedScope = new Scope
    {
        Id = Scope.ROOT_SCOPE_ID,
        Metadata = new ScopedMetadata("Root", EnumScopedType.Scope)
    };

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNewClasses;
    [ObservableProperty] private bool _hasExistingTasks;

    /// <summary>Header checkbox state for the "new classes" grid.</summary>
    [ObservableProperty] private bool _selectAllNewClasses;

    /// <summary>Header checkbox state for the "existing tasks" grid.</summary>
    [ObservableProperty] private bool _selectAllExistingTasks;

    /// <summary>True once loading completed and there is nothing to create nor update.</summary>
    [ObservableProperty] private bool _isEmpty;

    public PackageTasksVm(IScopedService scopedService,
        IPackagesService packagesService,
        NavigationManager navigation,
        ToastDisplay toasts,
        PackageInfos package)
    {
        _scopedService = scopedService;
        _packagesService = packagesService;
        _navigation = navigation;
        _toasts = toasts;
        _package = package;
    }

    public void OnShow() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var classes = await _packagesService.GetClassesAsync(
                _package.Identifier.Id, _package.Identifier.Version);

            var existingTasks = await _scopedService.GetTasksByPackageAsync(_package.Identifier.Id);

            // A class is "new" when no existing task already targets it.
            var targetedClasses = existingTasks
                .Where(t => t.Target != null)
                .Select(t => t.Target!.ClassFullName)
                .ToHashSet(StringComparer.Ordinal);

            NewClasses.Clear();
            foreach (var cls in classes.Where(c => !targetedClasses.Contains(c.ClassFullName)))
                NewClasses.Add(new NewClassRow(cls));

            ExistingTasks.Clear();
            foreach (var task in existingTasks)
                ExistingTasks.Add(new ExistingTaskRow(task));

            HasNewClasses = NewClasses.Count > 0;
            HasExistingTasks = ExistingTasks.Count > 0;
            IsEmpty = !HasNewClasses && !HasExistingTasks;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleNewClassesSelection(bool? isChecked)
    {
        bool value = isChecked ?? false;
        foreach (var row in NewClasses)
            row.IsSelected = value;
    }

    [RelayCommand]
    private void ToggleExistingTasksSelection(bool? isChecked)
    {
        bool value = isChecked ?? false;
        foreach (var row in ExistingTasks)
            row.IsSelected = value;
    }

    [RelayCommand]
    private async Task Apply()
    {
        var toCreate = NewClasses.Where(r => r.IsSelected).ToList();
        var toUpdate = ExistingTasks.Where(r => r.IsSelected).ToList();

        if (toCreate.Count == 0 && toUpdate.Count == 0)
        {
            _navigation.Close(this);
            return;
        }

        IsLoading = true;
        try
        {
            foreach (var row in toUpdate)
            {
                row.Task.Target!.Package.Version = TargetVersion;
                await _scopedService.EditAsync(row.Task);
            }

            var scopeId = SelectedScope?.Id ?? Scope.ROOT_SCOPE_ID;
            foreach (var row in toCreate)
            {
                string shortName = row.ClassFullName.Split('.').Last();
                var task = new AutomationTask(shortName, scopeId)
                {
                    Target = new PackageClassTarget(_package.Identifier, row.ClassFullName)
                    {
                        Dll = row.ClassTarget.Dll
                    }
                };
                await _scopedService.CreateAsync(task);
            }
        }
        finally
        {
            IsLoading = false;
        }

        var parts = new List<string>();
        if (toCreate.Count > 0) parts.Add($"{toCreate.Count} created");
        if (toUpdate.Count > 0) parts.Add($"{toUpdate.Count} updated");
        _toasts.Success("Tasks applied", string.Join(", ", parts) + ".");

        _navigation.Close(this);
    }

    [RelayCommand]
    private void Back() => _navigation.Close(this);
}

/// <summary>
/// Design class for <see cref="PackageTasksVm"/>.
/// </summary>
internal class PackageTasksVmDesign : PackageTasksVm
{
    public PackageTasksVmDesign() : base(null!, null!, null!, null!, new PackageInfos
    {
        Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.1.0") }
    })
    {
        var identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") };

        NewClasses.Add(new NewClassRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" }));
        NewClasses.Add(new NewClassRow(
            new PackageClassTarget(identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" }));

        ExistingTasks.Add(new ExistingTaskRow(new AutomationTask("HttpTask", Guid.NewGuid())
        {
            Target = new PackageClassTarget(identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" }
        }));

        HasNewClasses = true;
        HasExistingTasks = true;
        IsLoading = false;
    }
}
