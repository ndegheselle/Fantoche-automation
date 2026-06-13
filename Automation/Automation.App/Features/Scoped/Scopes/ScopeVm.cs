using System.Collections.ObjectModel;
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
        var children = await _scopedService.GetChildrens(Scope.Id);
        Children.Clear();
        foreach (var child in children)
        {
            Children.Add(From(child));
        }
    }

    public async Task AddChild(ScopedElement child)
    {
        child = await _scopedService.CreateAsync(child);
        Children.Add(From(child));
    }
}
