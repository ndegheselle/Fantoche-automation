using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Elements;

/// <summary>
/// View model wrapping a <see cref="Scope"/>, displayed by <see cref="ScopePage"/>.
/// </summary>
internal class ScopeVM : ScopedVM
{
    public Scope Scope => (Scope)Element;

    public ObservableCollection<ScopedVM> Children { get; set; } = [];

    public ScopeVM(Scope scope) : base(scope)
    {
    }

    public async Task LoadChildren()
    {
        var children = await _scopedService.GetChildrens(Scope.Id);
        Children.Clear();
        foreach (var child in children)
        {
            Children.Add(ScopedVM.From(child));
        }
    }
}
