using Automation.App.Common;
using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel : ScopedDetailsViewModel<Scope>
    {
        public Scope Scope => Element;
        public ScopeDetailsViewModel(ScopedNode node, WorkflowsViewModel parent, AppServices services)
            : base(node, parent, services)
        { }
    }
}
