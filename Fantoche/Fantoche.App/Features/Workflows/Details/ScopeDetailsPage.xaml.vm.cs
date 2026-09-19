using Fantoche.App.Common;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel : ScopedDetailsViewModel<Scope>
    {
        public Scope Scope => Element;
        public ScopeDetailsViewModel(ScopedNode node, WorkflowsViewModel parent, AppServices services)
            : base(node, parent, services)
        { }
    }
}
