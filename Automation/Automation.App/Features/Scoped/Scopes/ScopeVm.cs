using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Scoped.Scopes;

/// <summary>
/// View model wrapping a <see cref="Scope"/>, displayed by <see cref="ScopePage"/>.
/// </summary>
internal class ScopeVm : ScopedVm
{
    public Scope Scope => (Scope)Element;

    public ObservableCollection<ScopedVm> Children { get; set; } = [];

    public ScopeVm(Scope scope) : base(scope)
    {
    }

    public async Task LoadChildren()
    {
        if (Children.Count > 0)
            return;
        
        var children = await _scopedService.GetChildrens(Scope.Id);
        Children.Clear();
        // Group by type (scopes, then workflows, then tasks) and order alphabetically within each.
        foreach (var child in children
                     .OrderBy(c => c.Metadata.Type)
                     .ThenBy(c => c.Metadata.Name, StringComparer.OrdinalIgnoreCase))
        {
            Children.Add(From(child));
        }
    }

    public async Task AddChild(ScopedElement child)
    {
        child = await _scopedService.CreateAsync(child);
        var vm = From(child);
        Children.Insert(FindSortedIndex(vm), vm);
    }

    /// <summary>
    /// Index at which [item] should be inserted to keep <see cref="Children"/> ordered the same
    /// way <see cref="LoadChildren"/> orders them (by type, then name).
    /// </summary>
    private int FindSortedIndex(ScopedVm item)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            ScopedMetadata current = Children[i].Metadata;
            int byType = item.Metadata.Type.CompareTo(current.Type);
            int order = byType != 0
                ? byType
                : string.Compare(item.Metadata.Name, current.Name, StringComparison.OrdinalIgnoreCase);
            if (order < 0)
                return i;
        }
        return Children.Count;
    }
}
