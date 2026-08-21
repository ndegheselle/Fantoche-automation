using Automation.Shared.Data.Scoped;

namespace Automation.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel : ScopedDetailsViewModel<Scope>
    {
        public Scope Scope => Element;

        protected override string TypeName => "scope";

        public ScopeDetailsViewModel(ScopedNode node, WorkflowsViewModel parent) : base(node, parent)
        { }
    }
}
